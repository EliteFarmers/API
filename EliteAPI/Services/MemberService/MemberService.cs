using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Mappers.Skyblock;
using EliteAPI.Services.HypixelService;
using EliteAPI.Services.MojangService;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EliteAPI.Services.MemberService; 

public class MemberService : IMemberService {
    
    private readonly DataContext _context;
    private readonly IMojangService _mojangService;
    private readonly ConfigCooldownSettings _coolDowns;
    private readonly IHypixelService _hypixelService;
    private readonly ProfileParser _parser;
    private readonly IMapper _mapper;

    public MemberService(DataContext context, IMojangService mojangService, IHypixelService hypixelService, ProfileParser profileParser, IOptions<ConfigCooldownSettings> coolDowns, IMapper mapper) {
        _context = context;
        _mojangService = mojangService;
        _hypixelService = hypixelService;
        _parser = profileParser;
        _coolDowns = coolDowns.Value;
        _mapper = mapper;
    }
    
    public async Task<IQueryable<ProfileMember>?> ProfileMemberQuery(string playerUuid) {
        await UpdatePlayerIfNeeded(playerUuid);

        return _context.ProfileMembers.Where(p => p.PlayerUuid == playerUuid);
    }
    
    public async Task<IQueryable<ProfileMember>?> ProfileMemberCompleteQuery(string playerUuid) {
        await UpdatePlayerIfNeeded(playerUuid);

        return _context.ProfileMembers
            .AsNoTracking()
            .Where(p => p.PlayerUuid == playerUuid)
            .Include(p => p.MinecraftAccount).AsNoTracking()
            .Include(p => p.Profile).AsNoTracking()
            .Include(p => p.Skills).AsNoTracking()
            .Include(p => p.Inventories).AsNoTracking()
            .Include(p => p.Farming).AsNoTracking()
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .AsNoTracking()
            .AsSplitQuery();
    }

    public async Task<IQueryable<ProfileMember>?> ProfileMemberIgnQuery(string playerName) {
        var uuid = await _mojangService.GetUuidFromUsername(playerName);
        
        if (uuid is null) return null;
        
        return await ProfileMemberQuery(uuid);
    }

    public async Task UpdatePlayerIfNeeded(string playerUuid) {
        var account = await _mojangService.GetMinecraftAccountByUuid(playerUuid);
        if (account is null) return;
        
        var lastUpdated = new LastUpdatedDto {
            PlayerUuid = playerUuid,
            PlayerData = account.PlayerDataLastUpdated,
            Profiles = account.ProfilesLastUpdated
        };

        await RefreshNeededData(lastUpdated);
    }  
    
    public async Task UpdateProfileMemberIfNeeded(Guid memberId) {
        var lastUpdated = await _context.ProfileMembers
            .AsNoTracking()
            .Where(a => a.Id == memberId)
            .Include(a => a.MinecraftAccount)
            .AsNoTracking()
            .Select(a => new LastUpdatedDto {
                Profiles = a.MinecraftAccount.ProfilesLastUpdated,
                PlayerData = a.MinecraftAccount.PlayerDataLastUpdated,
                PlayerUuid = a.MinecraftAccount.Id
            })
            .FirstOrDefaultAsync();
        
        if (lastUpdated?.PlayerUuid is null) return;
        
        var account = await _mojangService.GetMinecraftAccountByUuid(lastUpdated.PlayerUuid);
        if (account is null) return;
        
        await RefreshNeededData(lastUpdated);
    }
    
    private async Task RefreshNeededData(LastUpdatedDto lastUpdated) {
        var playerUuid = lastUpdated.PlayerUuid;
        
        if (lastUpdated.Profiles.OlderThanSeconds(_coolDowns.SkyblockProfileCooldown)) {
            await RefreshProfiles(playerUuid);
        }
        
        if (lastUpdated.PlayerData.OlderThanSeconds(_coolDowns.HypixelPlayerDataCooldown)) {
            await RefreshPlayerData(playerUuid);
        }
    }

    public async Task RefreshProfiles(string playerUuid) {
        var data = await _hypixelService.FetchProfiles(playerUuid);

        if (data.Value is null) return;
        
        await _parser.TransformProfilesResponse(data.Value, playerUuid);
    }
    
    public async Task RefreshPlayerData(string playerUuid) {
        var data = await _hypixelService.FetchPlayer(playerUuid);
        var player = data.Value;

        if (player?.Player is null) return;

        var minecraftAccount = await _mojangService.GetMinecraftAccountByUuid(playerUuid);
        if (minecraftAccount is null) return;
        
        var existing = await _context.PlayerData.FirstOrDefaultAsync(a => a.Uuid == minecraftAccount.Id);
        var playerData = _mapper.Map<PlayerData>(player.Player);
        
        if (existing is null) {
            minecraftAccount.PlayerData = playerData;
            playerData.Uuid = minecraftAccount.Id;
            
            _context.PlayerData.Add(playerData);
        } else {
            _mapper.Map(playerData, existing);
            _context.PlayerData.Update(existing);
        }
        
        minecraftAccount.PlayerDataLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _context.SaveChangesAsync();
    }
}

public class LastUpdatedDto {
    public long Profiles { get; set; }
    public long PlayerData { get; set; }
    public required string PlayerUuid { get; set; }
}