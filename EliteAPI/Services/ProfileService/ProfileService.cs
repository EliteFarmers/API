using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.MemberService;
using EliteAPI.Services.MojangService;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Profile = EliteAPI.Models.Entities.Hypixel.Profile;

namespace EliteAPI.Services.ProfileService;

public class ProfileService : IProfileService
{
    private readonly DataContext _context;
    private readonly ConfigCooldownSettings _coolDowns;
    private readonly IMapper _mapper;
    
    private readonly IMojangService _mojangService;
    private readonly IMemberService _memberService;

    public ProfileService(DataContext context, 
        IMojangService mojangService, IMapper mapper,
        IOptions<ConfigCooldownSettings> coolDowns, 
        IMemberService memberService)
    {
        _context = context;
        _mojangService = mojangService;
        _memberService = memberService;
        _mapper = mapper;
        _coolDowns = coolDowns.Value;
    }

    public async Task<Profile?> GetProfile(string profileId)
    {
        return await _context.Profiles
            .AsNoTracking()
            .Include(p => p.Members)
            .ThenInclude(m => m.MinecraftAccount)
            .Include(p => p.Members)
            .ThenInclude(m => m.FarmingWeight)
            .FirstOrDefaultAsync(p => p.ProfileId.Equals(profileId));
    }

    public async Task<Profile?> GetPlayersProfileByName(string playerUuid, string profileName)
    {
        var query = await _memberService.ProfileMemberQuery(playerUuid);
        if (query is null) return null;
        
        var member = await query
            .Include(p => p.Profile)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Profile.ProfileName == profileName);

        return member?.Profile;
    }

    public async Task<Profile?> GetPlayersSelectedProfile(string playerUuid)
    {
        return (await GetSelectedProfileMember(playerUuid))?.Profile;
    }

    public async Task<List<Profile>> GetPlayersProfiles(string playerUuid)
    {
        await _memberService.UpdatePlayerIfNeeded(playerUuid);

        var profiles = await _context.ProfileMembers
            .Include(p => p.Profile)
            .Include(p => p.MinecraftAccount)
            .Select(p => p.Profile)
            .ToListAsync();

        return profiles;
    }

    public async Task<List<ProfileDetailsDto>> GetProfilesDetails(string playerUuid) {
        await _memberService.UpdatePlayerIfNeeded(playerUuid);
        
        var existing = await _context.ProfileMembers
            .AsNoTracking()
            .Where(m => m.PlayerUuid.Equals(playerUuid))
            .Select(m => new { m.ProfileId, m.IsSelected })
            .ToListAsync();
        
        var profileIds = existing.Select(e => e.ProfileId).ToList();

        var profiles = await _context.Profiles
            .AsNoTracking()
            .Include(p => p.Members)
            .ThenInclude(m => m.MinecraftAccount)
            .Include(p => p.Members)
            .ThenInclude(m => m.FarmingWeight)
            .Where(p => profileIds.Contains(p.ProfileId))
            .ToListAsync();
        
        var mappedProfiles = _mapper.Map<List<ProfileDetailsDto>>(profiles);

        // This needs to be fetched because "selected" lives on the ProfileMembers
        var selected = existing.FirstOrDefault(e => e.IsSelected)?.ProfileId;
        if (selected is not null) {
            mappedProfiles.ForEach(p => p.Selected = p.ProfileId == selected);
        }
        
        return mappedProfiles;
    }
    
    public async Task<ProfileMember?> GetProfileMember(string playerUuid, string profileUuid) {
        var query = await _memberService.ProfileMemberCompleteQuery(playerUuid);
        if (query is null) return null;

        return await query.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Profile.ProfileId.Equals(profileUuid));
    }

    public async Task<ProfileMember?> GetSelectedProfileMember(string playerUuid)
    {
        var query = await _memberService.ProfileMemberCompleteQuery(playerUuid);
        if (query is null) return null;

        return await query.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsSelected);
    }

    public async Task<ProfileMember?> GetProfileMemberByProfileName(string playerUuid, string profileName)
    {
        var query = await _memberService.ProfileMemberCompleteQuery(playerUuid);
        if (query is null) return null;

        return await query.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Profile.ProfileName == profileName);
    }

    public async Task<string?> GetSelectedProfileUuid(string playerUuid) {
        var query = await _memberService.ProfileMemberQuery(playerUuid);
        if (query is null) return null;
        
        return await query.AsNoTracking()
            .Where(p => p.IsSelected)
            .Select(p => p.ProfileId)
            .FirstOrDefaultAsync();
    }

    public async Task<PlayerData?> GetPlayerData(string playerUuid, bool skipCooldown = false)
    {
        await _memberService.UpdatePlayerIfNeeded(playerUuid);
        
        var data = await _context.PlayerData
            .AsNoTracking()
            .Include(p => p.MinecraftAccount)
            .FirstOrDefaultAsync(p => p.Uuid.Equals(playerUuid));
        
        if (data is not null && !data.LastUpdated.OlderThanSeconds(skipCooldown ? _coolDowns.HypixelPlayerDataCooldown : _coolDowns.HypixelPlayerDataLinkingCooldown))
                return data;

        await _memberService.RefreshPlayerData(playerUuid);
        
        return await _context.PlayerData
            .AsNoTracking()
            .Include(p => p.MinecraftAccount)
            .FirstOrDefaultAsync(p => p.Uuid.Equals(playerUuid));
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
}
