using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Parsers.Events;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services;

public class EventTeamService(
	IEventService eventService,
	DataContext context) 
	: IEventTeamService 
{
	public async Task<ActionResult<EventTeam>> CreateTeamAsync(CreateEventTeamDto team) {
		if (!ulong.TryParse(team.EventId, out var eventId)) {
			return new BadRequestObjectResult("Invalid event id");
		}
		
		var member = await eventService.GetEventMemberAsync(team.Owner, eventId);
		if (member is null) {
			return new BadRequestObjectResult("You are not a member of this event");
		}

		await context.Entry(member).Reference(m => m.Event).LoadAsync();
		if (member.Event.JoinUntilTime < DateTimeOffset.UtcNow) {
			return new BadRequestObjectResult("Event join time has expired");
		}
		
		if (!member.Event.IsCustomTeamEvent()) {
			return new BadRequestObjectResult("This event does not support custom teams");
		}

		var existing = await GetTeamAsync(team.Owner, eventId);
		if (existing is not null) {
			return new BadRequestObjectResult("You already have a team in this event");
		}
		
		member.Team = new EventTeam {
			Name = team.Name ?? "Unnamed Team",
			Color = team.Color,
			OwnerId = member.Id,
			EventId = eventId
		};
		
		await context.SaveChangesAsync();

		return member.Team;
	}

	public async Task<EventTeam?> GetTeamAsync(int id) {
		return await context.EventTeams
			.Include(t => t.Event)
			.Include(t => t.Members)
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.Id == id);
	}

	public async Task<EventTeam?> GetTeamAsync(int memberId, ulong eventId) {
		return await context.EventTeams
			.Include(t => t.Event)
			.Include(t => t.Members)
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.OwnerId == memberId && t.EventId == eventId);
	}

	public async Task<EventTeam?> GetTeamAsync(string playerUuidOrIgn, ulong eventId) {
		var member = await eventService.GetEventMemberAsync(playerUuidOrIgn, eventId);
		if (member?.TeamId is null) return null;

		await context.Entry(member).Reference(m => m.Team).LoadAsync();

		return member.Team;
	}

	public async Task<List<EventTeam>> GetEventTeamsAsync(ulong eventId) {
		return await context.EventTeams.AsNoTracking()
			.Include(t => t.Members)
			.Where(t => t.EventId == eventId)
			.ToListAsync();
	}

	public async Task<ActionResult> JoinTeamAsync(int teamId, string playerUuidOrIgn) {
		var team = await GetTeamAsync(teamId);
		if (team is null) {
			return new BadRequestObjectResult("Invalid team id");
		}
		
		if (team.Members.Count >= team.Event.MaxTeamMembers && team.Event.MaxTeamMembers != -1) {
			return new BadRequestObjectResult("Team is full");
		}
		
		if (team.Event.JoinUntilTime < DateTimeOffset.UtcNow) {
			return new BadRequestObjectResult("Event join time has expired");
		}

		var member = await eventService.GetEventMemberAsync(playerUuidOrIgn, team.EventId);
		if (member is null) {
			return new BadRequestObjectResult("You are not a member of this event");
		}

		if (member.TeamId is not null) {
			return new BadRequestObjectResult("You are already in a team");
		}

		member.TeamId = teamId;
		context.EventMembers.Update(member);
		
		await context.SaveChangesAsync();

		return new OkResult();
	}

	public async Task<ActionResult> LeaveTeamAsync(int teamId, string playerUuidOrIgn) {
		var team = await GetTeamAsync(teamId);
		if (team is null) {
			return new BadRequestObjectResult("Invalid team id");
		}
		
		if (team.Event.JoinUntilTime < DateTimeOffset.UtcNow) {
			return new BadRequestObjectResult("Event join time has expired");
		}
		
		var member = await eventService.GetEventMemberAsync(playerUuidOrIgn, team.EventId);
		if (member?.TeamId != teamId) {
			return new BadRequestObjectResult("You are not in this team");
		}

		member.TeamId = null;
		context.EventMembers.Update(member);
		
		await context.SaveChangesAsync();

		return new OkResult();
	}

	public async Task<ActionResult> KickMemberAsync(int teamId, string requester, string playerUuidOrIgn) {
		var team = await GetTeamAsync(teamId);
		if (team is null) {
			return new BadRequestObjectResult("Invalid team id");
		}
		
		if (team.Event.JoinUntilTime < DateTimeOffset.UtcNow) {
			return new BadRequestObjectResult("Event has already started");
		}
		
		var requesterMember = await eventService.GetEventMemberAsync(requester, team.EventId);
		if (requesterMember?.TeamId != teamId) {
			return new BadRequestObjectResult("You are not in this team");
		}
		
		if (requesterMember.Id != team.OwnerId) {
			return new BadRequestObjectResult("You are not the team owner");
		}
		
		var member = await eventService.GetEventMemberAsync(playerUuidOrIgn, team.EventId);
		if (member?.TeamId != teamId) {
			return new BadRequestObjectResult("This player is not in this team");
		}
		
		if (member.Id == team.OwnerId) {
			return new BadRequestObjectResult("You cannot kick the team owner");
		}
		
		member.TeamId = null;
		context.EventMembers.Update(member);
		
		await context.SaveChangesAsync();
		
		return new OkResult();
	}

	public async Task<ActionResult> UpdateTeamAsync(UpdateEventTeamDto team) {
		if (!ulong.TryParse(team.EventId, out var eventId)) {
			return new BadRequestObjectResult("Invalid event id");
		}
		
		var member = await eventService.GetEventMemberAsync(team.Owner, eventId);
		if (member?.TeamId is null) {
			return new BadRequestObjectResult("You are not a member of this event");
		}
		
		var existing = await GetTeamAsync(member.TeamId.Value);
		if (existing is null) {
			return new BadRequestObjectResult("Invalid team");
		}
		
		if (existing.OwnerId != member.Id) {
			return new BadRequestObjectResult("You are not the team owner");
		}
		
		existing.Name = team.Name ?? existing.Name;
		existing.Color = team.Color ?? existing.Color;
		
		context.EventTeams.Update(existing);
		await context.SaveChangesAsync();

		return new OkResult();
	}

	public async Task<ActionResult> DeleteTeamAsync(int id) {
		var team = await GetTeamAsync(id);
		if (team is null) {
			return new BadRequestObjectResult("Invalid team id");
		}
		
		if (team.Event.JoinUntilTime < DateTimeOffset.UtcNow) {
			return new BadRequestObjectResult("Event has already started");
		}
		
		context.EventTeams.Remove(team);
		await context.SaveChangesAsync();

		return new OkResult();
	}
}