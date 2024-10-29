using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.DTOs.Outgoing.Messaging;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services; 

public class GuildService(
    IDiscordService discordService,
    DataContext context,
    IMapper mapper,
    IMessageService messageService)
    : IGuildService 
{
    public async Task<ActionResult> SendLeaderboardPanel(ulong guildId, string channelId, string authorId, string lbId) {
        var guild = await discordService.GetGuild(guildId);

        if (guild is null) {
            return new NotFoundObjectResult("Guild not found.");
        }
        
        var channel = guild.Channels.FirstOrDefault(x => x.Id.ToString() == channelId);
        
        if (channel is null) {
            return new NotFoundObjectResult("Channel not found.");
        }
        
        var message = new MessageDto {
            Name = "leaderboardInit",
            GuildId = guildId.ToString(),
            AuthorId = authorId,
            Data = $$"""
                 {
                     "channelId": "{{channelId}}",
                     "leaderboardId": "{{lbId}}"
                 }
            """
        };
        
        messageService.SendMessage(message);

        return new OkResult();
    }

    public async Task UpdateGuildData(ulong guildId, IncomingGuildDto guild) {
        var dbGuild = await context.Guilds
            .Include(g => g.Channels)
            .Include(g => g.Roles)
            .Include(g => g.Icon)
            .Include(g => g.Banner)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.Id == guildId);

        var hasPerms = ulong.TryParse(guild.BotPermissions, out var botPermissions);
        
        if (dbGuild is null) {
            var icon = guild.Icon is not null ? await discordService.UpdateGuildIcon(guildId, guild.Icon) : null;
            var banner = guild.Banner is not null ? await discordService.UpdateGuildBanner(guildId, guild.Banner) : null;
            
            dbGuild = new Guild {
                Id = guildId,
                Name = guild.Name,
                Icon = icon,
                Banner = banner,
                BotPermissions = hasPerms ? botPermissions : 0,
                DiscordFeatures = guild.Features ?? []
            };
            await context.Guilds.AddAsync(dbGuild);
        } else {
            dbGuild.Name = guild.Name;
            dbGuild.BotPermissions = hasPerms ? botPermissions : dbGuild.BotPermissions;
            dbGuild.DiscordFeatures = guild.Features ?? dbGuild.DiscordFeatures;
            
            if (guild.Icon is not null && guild.Icon != dbGuild.Icon?.Hash) {
                dbGuild.Icon = await discordService.UpdateGuildIcon(guildId, guild.Icon);
            }
        }

        await context.SaveChangesAsync();

        if (guild.Channels is not null) {
            foreach (var channel in guild.Channels) {
                await UpdateGuildChannelData(guildId, channel, true);
            }
            
            await context.SaveChangesAsync();
        }

        if (guild.Roles is not null) {
            foreach (var role in guild.Roles) {
                await UpdateGuildRoleData(guildId, role, true);
            }
            
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateGuildChannelData(ulong guildId, IncomingGuildChannelDto channel, bool skipSave = false) {
        if (!ulong.TryParse(channel.Id, out var channelId)) {
            return;
        }
        
        var dbChannel = await context.GuildChannels
            .FirstOrDefaultAsync(c => c.Id == channelId && c.GuildId == guildId);

        var hasPerms = ulong.TryParse(channel.Permissions, out var perms);

        if (dbChannel is null) {
            dbChannel = new GuildChannel {
                Id = channelId,
                Name = channel.Name,
                Type = channel.Type,
                Position = channel.Position,
                BotPermissions = hasPerms ? perms : 0,
                GuildId = guildId
            };
            await context.GuildChannels.AddAsync(dbChannel);
        } else {
            dbChannel.Name = channel.Name;
            dbChannel.Type = channel.Type != 0 ? channel.Type : dbChannel.Type;
            dbChannel.Position = channel.Position != 0 ? channel.Position : dbChannel.Position;
            dbChannel.BotPermissions = hasPerms ? perms : dbChannel.BotPermissions;
            dbChannel.LastUpdated = DateTimeOffset.UtcNow;
        }

        if (!skipSave) {
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateGuildRoleData(ulong guildId, IncomingGuildRoleDto role, bool skipSave = false) {
        if (!ulong.TryParse(role.Id, out var roleId)) {
            return;
        }
        
        var dbRole = await context.GuildRoles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.GuildId == guildId);

        if (dbRole is null) {
            dbRole = new GuildRole {
                Id = roleId,
                Name = role.Name,
                Position = role.Position,
                GuildId = guildId
            };
            await context.GuildRoles.AddAsync(dbRole);
        } else {
            dbRole.Name = role.Name;
            dbRole.Position = role.Position;
            dbRole.LastUpdated = DateTimeOffset.UtcNow;
        }
    
        if (!skipSave) {
            await context.SaveChangesAsync();
        }
    }

    public async Task<GuildMemberDto?> GetUserGuild(string userId, ulong guildId) {
        var guildMember = await context.GuildMembers
            .Include(u => u.Guild)
            .ThenInclude(guild => guild.Icon)
            .FirstOrDefaultAsync(u => u.AccountId == userId && u.GuildId == guildId);
        
        return guildMember is null ? null : mapper.Map<GuildMemberDto>(guildMember);
    }

    public async Task<GuildMemberDto?> GetUserGuild(ApiUser user, ulong guildId) {
        return await GetUserGuild(user.Id, guildId);
    }
}