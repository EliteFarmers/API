using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.User.JoinEvent;

internal sealed class JoinEventEndpoint(
	DataContext context,
    IEventService eventService,
    UserManager userManager,
    IDiscordService discordService,
    IMemberService memberService)
	: Endpoint<JoinEventRequest>
{
	public override void Configure() {
		Post("/event/{EventId}/join");
		Version(0);
        
        Description(x => x.Accepts<JoinEventRequest>());

		Summary(s => {
			s.Summary = "Join an event";
		});
	}

	public override async Task HandleAsync(JoinEventRequest request, CancellationToken c) {
		var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null || user.DiscordAccessToken is null) {
            await SendUnauthorizedAsync(c);
            return;
        }
        
        var eliteEvent = await context.Events
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken: c);
        
        if (eliteEvent is null) {
            await SendNotFoundAsync(c);
            return;
        }

        await context.Entry(user).Reference(x => x.Account).LoadAsync(c);
        await context.Entry(user.Account).Collection(x => x.MinecraftAccounts).LoadAsync(c);
        var account = user.Account;
        
        if (DateTimeOffset.UtcNow > eliteEvent.JoinUntilTime || DateTimeOffset.UtcNow > eliteEvent.EndTime) {
            ThrowError("Event has ended or join time has passed.");
        }
        
        var guilds = await discordService.GetUsersGuilds(user.Id);
        var userGuild = guilds.FirstOrDefault(g => g.Id == eliteEvent.GuildId.ToString());

        if (userGuild is null) {
            ThrowError("You need to be in the event's Discord server in order to join!");
        }
        
        if (eliteEvent.RequiredRole is not null && !userGuild.Roles.Contains(eliteEvent.RequiredRole)) {
            ThrowError("You need to have the required role in the event's Discord server in order to join!");
        }
        
        if (eliteEvent.BlockedRole is not null && userGuild.Roles.Contains(eliteEvent.BlockedRole)) {
            ThrowError("You have a blocked role in the event's Discord server and cannot join!");
        }
        
        var selectedAccount = request.PlayerUuidFormatted.IsNullOrEmpty()
            ? account.MinecraftAccounts.FirstOrDefault(a => a.Selected)
            : account.MinecraftAccounts.FirstOrDefault(a => a.Id == request.PlayerUuidFormatted);
        
        if (selectedAccount is null) {
            ThrowError("You need to have a linked Minecraft account in order to join!");
        }
        
        var member = await context.EventMembers.AsNoTracking()
            .Include(e => e.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == request.EventId && e.ProfileMember.PlayerUuid == selectedAccount.Id, cancellationToken: c);
        
        if (member is not null && member.Status != EventMemberStatus.Left) {
            ThrowError("You are already a member of this event!");
        }

        var updateQuery = await memberService.ProfileMemberQuery(selectedAccount.Id);

        if (updateQuery is null) {
            ThrowError("Profile member data not found, please try again later or ensure an existing profile is selected.");
        }

        var query = updateQuery
            .Include(p => p.MinecraftAccount).AsNoTracking()
            .Include(p => p.Farming).AsNoTracking()
            .Where(p => p.PlayerUuid == selectedAccount.Id);
            
        var profileMember = (request.ProfileUuidFormatted.IsNullOrEmpty()) 
            ? await query.FirstOrDefaultAsync(a => a.IsSelected, cancellationToken: c)
            : await query.FirstOrDefaultAsync(a => a.ProfileId == request.ProfileUuidFormatted, cancellationToken: c);

        if (profileMember?.Farming is null) {
            ThrowError("Profile member data not found, please try again later or ensure an existing profile is selected.");
        }
        
        if (!profileMember.Api.Collections) {
            ThrowError("Collections API needs to be enabled in order to join this event.");
        }

        if (eliteEvent.Type == EventType.FarmingWeight) {
            if (!profileMember.Api.Inventories) {
                ThrowError("Inventories API needs to be enabled in order to join this event.");
            }
            
            if (profileMember.Farming.Inventory?.Tools is null || profileMember.Farming.Inventory.Tools.Count == 0) {
                ThrowError("You need to have at least one farming tool in your inventory in order to join this event.");
            }
        }
        
        var eventActive = eliteEvent.StartTime < DateTimeOffset.UtcNow && eliteEvent.EndTime > DateTimeOffset.UtcNow;
        if (eventActive != eliteEvent.Active) {
            eliteEvent.Active = eventActive;
        }
        
        // Rejoin event if left previously
        if (member is not null) {
            member.Status = eventActive ? EventMemberStatus.Active : EventMemberStatus.Inactive;
            context.Entry(member).State = EntityState.Modified;
            
            await context.SaveChangesAsync(c);
            
            // Init member if needed
            await eventService.InitializeEventMember(member, eliteEvent, profileMember);
            
            await SendNoContentAsync(cancellation: c);
            return;
        }
        
        try {
            await eventService.CreateEventMember(eliteEvent, new CreateEventMemberDto {
                EventId = eliteEvent.Id,
                ProfileMemberId = profileMember.Id,
                UserId = account.Id,
                Score = 0,
                StartTime = eliteEvent.StartTime,
                EndTime = eliteEvent.EndTime,
                ProfileMember = profileMember
            });

            await context.SaveChangesAsync(c);
        } catch (DbUpdateException) {
            ThrowError("Player (or linked account) is already in the event.");
        }

		await SendNoContentAsync(cancellation: c);
	}
}
