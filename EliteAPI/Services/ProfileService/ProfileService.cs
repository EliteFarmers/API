using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Skyblock;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.HypixelService;
using EliteAPI.Services.MojangService;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Profile = EliteAPI.Models.Entities.Hypixel.Profile;

namespace EliteAPI.Services.ProfileService;

public class ProfileService : IProfileService
{
    private readonly DataContext _context;
    private readonly ProfileParser _profileParser;
    private readonly ConfigCooldownSettings _coolDowns;

    private readonly IHypixelService _hypixelService;
    private readonly IMojangService _mojangService;
    private readonly IMapper _mapper;

    public ProfileService(DataContext context, 
        IHypixelService hypixelService, IMojangService mojangService, 
        ProfileParser profileParser, IMapper mapper,
        IOptions<ConfigCooldownSettings> coolDowns)
    {
        _context = context;
        _hypixelService = hypixelService;
        _mojangService = mojangService;
        _profileParser = profileParser;
        _mapper = mapper;
        _coolDowns = coolDowns.Value;
    }

    public async Task<Profile?> GetProfile(string profileId)
    {
        return await _context.Profiles
            .Include(p => p.Members)
            .ThenInclude(m => m.MinecraftAccount)
            .Include(p => p.Members)
            .ThenInclude(m => m.FarmingWeight)
            .FirstOrDefaultAsync(p => p.ProfileId.Equals(profileId));
    }

    public async Task<Profile?> GetPlayersProfileByName(string playerUuid, string profileName)
    {
        var member = await _context.ProfileMembers
            .Include(p => p.Profile)
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.Profile.ProfileName.Equals(profileName))
            .FirstOrDefaultAsync();

        return member?.Profile;
    }

    public async Task<Profile?> GetPlayersSelectedProfile(string playerUuid)
    {
        return (await GetSelectedProfileMember(playerUuid))?.Profile;
    }

    public async Task<List<Profile>> GetPlayersProfiles(string playerUuid)
    {
        var profileIds = await _context.ProfileMembers
            .Where(m => m.PlayerUuid.Equals(playerUuid))
            .Select(m => m.ProfileId)
            .ToListAsync();
        
        if (profileIds.Count == 0)
        {
            await RefreshProfileMembers(playerUuid);

            profileIds = await _context.ProfileMembers
                .Where(m => m.PlayerUuid.Equals(playerUuid))
                .Select(m => m.ProfileId)
                .ToListAsync();
            
            if (profileIds.Count == 0) return new List<Profile>();
        }

        var profiles = await _context.Profiles
            .Include(p => p.Members)
            .ThenInclude(m => m.MinecraftAccount)
            .Where(p => profileIds.Contains(p.ProfileId))
            .ToListAsync();

        return profiles;
    }

    public async Task<List<ProfileDetailsDto>> GetProfilesDetails(string playerUuid) {
        var existing = await _context.ProfileMembers
            .Where(m => m.PlayerUuid.Equals(playerUuid))
            .Select(m => new { m.ProfileId, m.LastUpdated })
            .ToListAsync();
    
        if (existing.Count == 0 || existing.Any(e => e.LastUpdated.OlderThanSeconds(_coolDowns.SkyblockProfileCooldown)))
        {
            await RefreshProfileMembers(playerUuid);
        }
        
        var profileIds = existing.Select(e => e.ProfileId).ToList();

        var profiles = await _context.Profiles
            .Include(p => p.Members)
            .ThenInclude(m => m.MinecraftAccount)
            .Include(p => p.Members)
            .ThenInclude(m => m.FarmingWeight)
            .Where(p => profileIds.Contains(p.ProfileId))
            .ToListAsync();
        
        var mappedProfiles = _mapper.Map<List<ProfileDetailsDto>>(profiles);

        // This needs to be fetched because "selected" lives on the ProfileMembers
        var selected = await GetSelectedProfileUuid(playerUuid);
        if (selected is not null) {
            mappedProfiles.ForEach(p => p.Selected = p.ProfileId == selected);
        }
        
        return mappedProfiles;
    }

    private readonly Func<DataContext, string, string, Task<ProfileMember?>> _fetchProfileMemberData = 
        EF.CompileAsyncQuery((DataContext context, string profileUuid, string playerUuid) =>            
            context.ProfileMembers
                   .Include(p => p.Profile)
                   .Include(p => p.Skills)
                   .Include(p => p.FarmingWeight)
                   .Include(p => p.JacobData)
                   .ThenInclude(j => j.Contests)
                   .ThenInclude(c => c.JacobContest)
                   .AsSplitQuery()
                   .FirstOrDefault(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
        );

    public async Task<ProfileMember?> GetProfileMember(string profileUuid, string playerUuid)
    {
        var lastUpdated = await _context.ProfileMembers
            .Include(p => p.Profile)
            .Where(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
            .Select(p => p.LastUpdated)
            .FirstOrDefaultAsync();

        // Fetch new data if it doesn't exists or is old
        if (lastUpdated.OlderThanSeconds(_coolDowns.SkyblockProfileCooldown))
        {
            await RefreshProfileMembers(playerUuid);
        }

        return await _fetchProfileMemberData(_context, profileUuid, playerUuid);
    }

    public async Task<ProfileMember?> GetSelectedProfileMember(string playerUuid)
    {
        var member = await _context.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.IsSelected)
            .FirstOrDefaultAsync();

        if (member is null || member.LastUpdated.OlderThanSeconds(_coolDowns.SkyblockProfileCooldown))
        {
            var members = await RefreshProfileMembers(playerUuid);
            return members.FirstOrDefault(m => m.IsSelected);
        }

        return await _fetchProfileMemberData(_context, member.ProfileId, playerUuid);
    }

    public async Task<ProfileMember?> GetProfileMemberByProfileName(string playerUuid, string profileName)
    {
        var member = await _context.ProfileMembers
            .Include(p => p.Profile)
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.Profile.ProfileName.Equals(profileName))
            .FirstOrDefaultAsync();

        if (member is null || member.LastUpdated.OlderThanSeconds(_coolDowns.SkyblockProfileCooldown))
        {
            var members = await RefreshProfileMembers(playerUuid);
            return members.FirstOrDefault(m => m.Profile.ProfileName.Equals(profileName));
        }

        return await _fetchProfileMemberData(_context, member.ProfileId, playerUuid);
    }

    public async Task<string?> GetSelectedProfileUuid(string playerUuid) {
        return await _context.ProfileMembers
            .Where(s => s.PlayerUuid == playerUuid && s.IsSelected)
            .Select(s => s.ProfileId)
            .FirstOrDefaultAsync();
    }

    public async Task<PlayerData?> GetPlayerData(string playerUuid, bool skipCooldown = false)
    {
        var data = await _context.PlayerData
            .Include(p => p.MinecraftAccount)
            .FirstOrDefaultAsync(p => p.Uuid.Equals(playerUuid));
        
        if (data is not null && !data.LastUpdated.OlderThanSeconds(skipCooldown ? _coolDowns.HypixelPlayerDataCooldown : _coolDowns.HypixelPlayerDataLinkingCooldown))
                return data;

        var rawData = await _hypixelService.FetchPlayer(playerUuid);
        var player = rawData.Value;

        if (player?.Player is null) return null;

        var minecraftAccount = await _mojangService.GetMinecraftAccountByUuid(playerUuid);
        if (minecraftAccount is null) return null;

        var playerData = _mapper.Map<PlayerData>(player.Player);
        playerData.MinecraftAccount = minecraftAccount;

        _context.PlayerData.Add(playerData);
        await _context.SaveChangesAsync();

        return playerData;
    }

    public async Task<PlayerData?> GetPlayerDataByIgn(string playerName, bool skipCooldown = false)
    {
        var uuid = await _mojangService.GetUuidFromUsername(playerName);
        if (uuid is null) return null;

        return await GetPlayerData(uuid, skipCooldown);
    }

    public async Task<PlayerData?> GetPlayerDataByUuidOrIgn(string uuidOrIgn, bool skipCooldown = false)
    {
        if (uuidOrIgn.Length == 32) return await GetPlayerData(uuidOrIgn);
        return await GetPlayerDataByIgn(uuidOrIgn, skipCooldown);
    }

    private async Task<List<ProfileMember>> RefreshProfileMembers(string playerUuid)
    {
        var rawData = await _hypixelService.FetchProfiles(playerUuid);
        var profiles = rawData.Value;

        if (profiles == null) return new List<ProfileMember>();

        return await _profileParser.TransformProfilesResponse(profiles, playerUuid);
    }
}
