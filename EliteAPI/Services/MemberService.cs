using AutoMapper;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services; 

public class MemberService(
    DataContext context,
    IServiceScopeFactory provider,
    IMojangService mojangService,
    IOptions<ConfigCooldownSettings> coolDowns,
    IConnectionMultiplexer redis)
    : IMemberService 
{
    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;

    public async Task<Guid?> GetProfileMemberId(string playerUuid, string profileId) {
        return await context.ProfileMembers
            .AsNoTracking()
            .Where(p => p.PlayerUuid == playerUuid && p.ProfileId == profileId)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<IQueryable<ProfileMember>?> ProfileMemberQuery(string playerUuid, float cooldownMultiplier = 1) {
        await UpdatePlayerIfNeeded(playerUuid);

        return context.ProfileMembers.Where(p => p.PlayerUuid == playerUuid);
    }
    
    public async Task<IQueryable<ProfileMember>?> ProfileMemberCompleteQuery(string playerUuid, float cooldownMultiplier = 1) {
        await UpdatePlayerIfNeeded(playerUuid);

        var now = DateTimeOffset.UtcNow;
        return context.ProfileMembers
            .AsNoTracking()
            .Where(p => p.PlayerUuid == playerUuid)
            .Include(p => p.MinecraftAccount)
            .Include(p => p.Profile)
            .ThenInclude(p => p.Garden)
            .Include(p => p.Skills)
            .Include(p => p.Farming)
            .Include(p => p.EventEntries!
                .Where(e => 
                    (e.Status == EventMemberStatus.Active || e.Status == EventMemberStatus.Inactive) 
                    && e.EndTime > now && e.StartTime <= now))
            .ThenInclude(m => m.Event)
            .Include(p => p.ChocolateFactory).AsNoTracking()
            .Include(p => p.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .AsNoTracking()
            .AsSplitQuery();
    }

    public async Task<IQueryable<ProfileMember>?> ProfileMemberIgnQuery(string playerName) {
        var uuid = await mojangService.GetUuidFromUsername(playerName);
        
        if (uuid is null) return null;
        
        return await ProfileMemberQuery(uuid);
    }

    public async Task UpdatePlayerIfNeeded(string playerUuid, float cooldownMultiplier = 1) {
        var account = await mojangService.GetMinecraftAccountByUuidOrIgn(playerUuid);
        if (account is null) return;
        
        var lastUpdated = new LastUpdatedDto {
            PlayerUuid = account.Id,
            PlayerData = account.PlayerDataLastUpdated,
            Profiles = account.ProfilesLastUpdated
        };

        await RefreshNeededData(lastUpdated, account, cooldownMultiplier);
    }  
    
    public async Task UpdateProfileMemberIfNeeded(Guid memberId, float cooldownMultiplier = 1) {
        var lastUpdated = await context.ProfileMembers
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
        
        var account = await mojangService.GetMinecraftAccountByUuid(lastUpdated.PlayerUuid);
        if (account is null) return;
        
        await RefreshNeededData(lastUpdated, account, cooldownMultiplier);
    }
    
    private async Task RefreshNeededData(LastUpdatedDto lastUpdated, MinecraftAccount account, float cooldownMultiplier = 1) {
        var playerUuid = lastUpdated.PlayerUuid;
        var db = redis.GetDatabase();
        
        var updatePlayer = false;
        var updateProfiles = false;
        
        if (lastUpdated.Profiles.OlderThanSeconds((int) (_coolDowns.SkyblockProfileCooldown * cooldownMultiplier))
            && !await db.KeyExistsAsync($"profile:{playerUuid}:updating"))
        {
            db.StringSet($"profile:{playerUuid}:updating", "1", TimeSpan.FromSeconds(15));
            updateProfiles = true;
        }
        
        if (lastUpdated.PlayerData.OlderThanSeconds((int) (_coolDowns.HypixelPlayerDataCooldown * cooldownMultiplier)) 
            && !await db.KeyExistsAsync($"player:{playerUuid}:updating")) 
        {
            db.StringSet($"player:{playerUuid}:updating", "1", TimeSpan.FromSeconds(15));
            updatePlayer = true;
        }

        if (updateProfiles) {
            await RefreshProfiles(playerUuid);
        }
        
        if (updatePlayer) {
            await RefreshPlayerData(playerUuid, account);
        }
    }

    public async Task RefreshProfiles(string playerUuid) {
        using var scope = provider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IProfileProcessorService>();
        var hypixelService = scope.ServiceProvider.GetRequiredService<IHypixelService>();
        
        var data = await hypixelService.FetchProfiles(playerUuid);

        if (data.Value is null) {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MemberService>>();
            logger.LogError("Failed to load profiles for {PlayerUuid}", playerUuid);
            return;
        }
        
        try {
            await processor.ProcessProfilesWaitForOnePlayer(data.Value, playerUuid);
        } catch (Exception e) {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MemberService>>();
            logger.LogError(e, "Failed to process profiles for {PlayerUuid}", playerUuid);
        }
    }
    
    public async Task RefreshPlayerData(string playerUuid, MinecraftAccount? account = null) {
        using var scope = provider.CreateScope();
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