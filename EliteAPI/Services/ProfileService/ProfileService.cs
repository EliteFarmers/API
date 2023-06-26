using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Mappers.Skyblock;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.HypixelService;
using EliteAPI.Services.MojangService;
using Microsoft.EntityFrameworkCore;
using Profile = EliteAPI.Models.Entities.Hypixel.Profile;

namespace EliteAPI.Services.ProfileService;

public class ProfileService : IProfileService
{
    private readonly DataContext _context;
    private readonly ProfileParser _profileParser;

    private readonly IHypixelService _hypixelService;
    private readonly IMojangService _mojangService;
    private readonly IMapper _mapper;

    public ProfileService(DataContext context, IHypixelService hypixelService, IMojangService mojangService, ProfileParser profileParser, IMapper mapper)
    {
        _context = context;
        _hypixelService = hypixelService;
        _mojangService = mojangService;
        _profileParser = profileParser;
        _mapper = mapper;
    }

    public async Task<Profile?> GetProfile(string profileId)
    {
        return await _context.Profiles
            .Include(p => p.Members)
            .ThenInclude(m => m.MinecraftAccount)
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

    private readonly Func<DataContext, string, string, Task<ProfileMember?>> _fetchProfileMemberData = 
        EF.CompileAsyncQuery((DataContext context, string profileUuid, string playerUuid) =>            
            context.ProfileMembers
                   .Include(p => p.Profile)
                   .Include(p => p.Skills)
                   .Include(p => p.JacobData)
                   //.ThenInclude(j => j.Contests)
                   .AsSplitQuery()
                   .FirstOrDefault(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
        );

    public async Task<ProfileMember?> GetProfileMember(string profileUuid, string playerUuid)
    {
        var member = await _context.ProfileMembers
            .Include(p => p.Profile)
            .Where(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
            .FirstOrDefaultAsync();

        // Fetch new data if it doesn't exists or is old
        if (member == null || member.LastUpdated + 600 < DateTimeOffset.UtcNow.ToUnixTimeSeconds() || true)
        {
            await RefreshProfileMembers(playerUuid);
        }

        return await _context.ProfileMembers
            .Include(p => p.Profile)
            .Include(p => p.Skills)
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .AsSplitQuery()
            .Where(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
            .FirstOrDefaultAsync();
    }

    public async Task<ProfileMember?> GetSelectedProfileMember(string playerUuid)
    {
        var member = await _context.ProfileMembers
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.IsSelected)
            .FirstOrDefaultAsync();

        if (member == null || true)
        {
            var data = await RefreshProfileMembers(playerUuid);

            return data.FirstOrDefault(p => p.IsSelected);
        }

        return null;
    }

    public async Task<ProfileMember?> GetProfileMemberByProfileName(string playerUuid, string profileName)
    {
        return await _context.ProfileMembers
            .Include(p => p.Profile)
            .Where(p => p.PlayerUuid.Equals(playerUuid) && p.Profile.ProfileName.Equals(profileName))
            .FirstOrDefaultAsync();
    }

    public async Task<PlayerData?> GetPlayerData(string playerUuid, bool skipCooldown = false)
    {
        var data = await _context.PlayerData
            .Include(p => p.MinecraftAccount)
            .FirstOrDefaultAsync(p => p.Uuid.Equals(playerUuid));

        // 3 day cooldown
        if (data is not null && data.LastUpdated + (skipCooldown ? 259_200 : 30) >= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return data;

        var rawData = await _hypixelService.FetchPlayer(playerUuid);
        var player = rawData.Value;

        if (player?.Player is null) return null;

        var minecraftAccount = await _context.MinecraftAccounts.FindAsync(playerUuid);
        if (minecraftAccount is null) return null;

        var playerData = _mapper.Map<PlayerData>(player.Player);
        playerData.MinecraftAccount = minecraftAccount;

        _context.PlayerData.Add(playerData);
        await _context.SaveChangesAsync();

        return playerData;
    }

    public async Task<PlayerData?> GetPlayerDataByIgn(string playerName, bool skipCooldown = false)
    {
        var minecraftAccount = await _mojangService.GetMinecraftAccountByIgn(playerName);
        if (minecraftAccount is null) return null;

        return await GetPlayerData(minecraftAccount.Id, skipCooldown);
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
