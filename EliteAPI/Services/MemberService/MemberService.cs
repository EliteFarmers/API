using AutoMapper;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.Entities;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Skyblock;
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
    private readonly ILogger<MemberService> _logger;
    private readonly IServiceScopeFactory _provider;

    public MemberService(DataContext context, IServiceScopeFactory provider, IMojangService mojangService, IHypixelService hypixelService, ProfileParser profileParser, IOptions<ConfigCooldownSettings> coolDowns, IMapper mapper, ILogger<MemberService> logger) {
        _context = context;
        _provider = provider;
        _mojangService = mojangService;
        _hypixelService = hypixelService;
        _parser = profileParser;
        _coolDowns = coolDowns.Value;
        _mapper = mapper;
        _logger = logger;
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
        var account = await _mojangService.GetMinecraftAccountByUuidOrIgn(playerUuid);
        if (account is null) return;
        
        var lastUpdated = new LastUpdatedDto {
            PlayerUuid = playerUuid,
            PlayerData = account.PlayerDataLastUpdated,
            Profiles = account.ProfilesLastUpdated
        };

        await RefreshNeededData(lastUpdated, account);
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
        
        await RefreshNeededData(lastUpdated, account);
    }
    
    private async Task RefreshNeededData(LastUpdatedDto lastUpdated, MinecraftAccount account) {
        var playerUuid = lastUpdated.PlayerUuid;
        
        // Refresh tasks are done in separate scopes to prevent DataContext concurrency issues
        
        var tasks = new List<Task>();
        if (lastUpdated.Profiles.OlderThanSeconds(_coolDowns.SkyblockProfileCooldown)) {
            tasks.Add(RefreshProfiles(playerUuid));
        }
        
        if (lastUpdated.PlayerData.OlderThanSeconds(_coolDowns.HypixelPlayerDataCooldown)) {
            tasks.Add(RefreshPlayerData(playerUuid, account));
        }
        
        await Task.WhenAll(tasks);
    }

    public async Task RefreshProfiles(string playerUuid) {
        using var scope = _provider.CreateScope();
        var parser = scope.ServiceProvider.GetRequiredService<ProfileParser>();
        var hypixelService = scope.ServiceProvider.GetRequiredService<IHypixelService>();
        
        var data = await hypixelService.FetchProfiles(playerUuid);

        if (data.Value is null) return;
        
        await parser.TransformProfilesResponse(data.Value, playerUuid);
    }
    
    public async Task RefreshPlayerData(string playerUuid, MinecraftAccount? account = null) {
        using var scope = _provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var mojangService = scope.ServiceProvider.GetRequiredService<IMojangService>();
        var hypixelService = scope.ServiceProvider.GetRequiredService<IHypixelService>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        
        var data = await hypixelService.FetchPlayer(playerUuid);
        var player = data.Value;

        if (player?.Player is null) return;

        var minecraftAccount = account ?? await mojangService.GetMinecraftAccountByUuid(playerUuid);
        if (minecraftAccount is null) return;
        
        var existing = await context.PlayerData.FirstOrDefaultAsync(a => a.Uuid == minecraftAccount.Id);
        var playerData = mapper.Map<PlayerData>(player.Player);
        
        if (existing is null) {
            playerData.Uuid = minecraftAccount.Id;

            minecraftAccount.PlayerData = playerData;
            minecraftAccount.PlayerDataLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            context.PlayerData.Add(playerData);
        } else {
            mapper.Map(playerData, existing);

            if (existing.MinecraftAccount is not null) {
                existing.MinecraftAccount.PlayerDataLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            } else {
                minecraftAccount.PlayerDataLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (context.Entry(minecraftAccount).State == EntityState.Detached) {
                    context.Entry(minecraftAccount).State = EntityState.Modified;
                }
            }
            
            context.PlayerData.Update(existing);
        }
        
        await context.SaveChangesAsync();
    }
}

public class LastUpdatedDto {
    public long Profiles { get; set; }
    public long PlayerData { get; set; }
    public required string PlayerUuid { get; set; }
}