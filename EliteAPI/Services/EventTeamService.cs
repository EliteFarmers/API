using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Parsers.Events;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EliteAPI.Services;

public class EventTeamService(
	IEventService eventService,
	IOptions<ConfigEventSettings> config,
	DataContext context) 
	: IEventTeamService 
{
	private readonly ImmutableList<string> _eventTeamWordList = 
		ImmutableList.CreateRange(config.Value.TeamsWordList.Adjectives
			.Concat(config.Value.TeamsWordList.Nouns)
			.Concat(config.Value.TeamsWordList.Verbs));
	
	public async Task<ActionResult> CreateTeamAsync(ulong eventId, CreateEventTeamDto team, string userId) {
		var member = await eventService.GetEventMemberAsync(userId, eventId);
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

		var existing = await GetTeamAsync(member.UserId.ToString(), eventId);
		if (existing is not null) {
			return new BadRequestObjectResult("You already have a team in this event");
		}

		if (!IsValidTeamName(team.Name)) {
			team.Name = GetRandomTeamName();
		}
		
		member.Team = new EventTeam {
			Name = team.Name,
			Color = team.Color,
			UserId = userId,
			EventId = eventId
		};

		context.EventMembers.Update(member);
		await context.SaveChangesAsync();

		return new OkResult();
	}
	
	public async Task<ActionResult> DeleteTeamValidateAsync(int id) {
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

	public async Task<ActionResult> DeleteTeamAsync(int id) {
		var team = await GetTeamAsync(id);
		if (team is null) {
			return new BadRequestObjectResult("Invalid team id");
		}
		
		context.EventTeams.Remove(team);
		await context.SaveChangesAsync();

		return new OkResult();
	}

	public async Task<EventTeam?> GetTeamAsync(int id) {
		return await context.EventTeams
			.Include(t => t.Event)
			.Include(t => t.Members)
			.ThenInclude(m => m.ProfileMember)
			.ThenInclude(p => p.MinecraftAccount)
			.AsNoTracking().AsSplitQuery()
			.FirstOrDefaultAsync(t => t.Id == id);
	}

	public async Task<EventTeam?> GetTeamAsync(string userId, ulong eventId) {
		return await context.EventTeams
			.Include(t => t.Event)
			.Include(t => t.Members)
			.ThenInclude(m => m.ProfileMember)
			.ThenInclude(p => p.MinecraftAccount)
			.AsNoTracking().AsSplitQuery()
			.FirstOrDefaultAsync(t => t.UserId == userId && t.EventId == eventId);
	}

	public async Task<List<EventTeam>> GetEventTeamsAsync(ulong eventId) {
		var teams = await context.EventTeams
			.Include(t => t.Members)
			.ThenInclude(m => m.ProfileMember)
			.ThenInclude(p => p.MinecraftAccount)
			.Where(t => t.EventId == eventId)
			.AsNoTracking().AsSplitQuery()
			.ToListAsync();

		return teams.OrderByDescending(t => t.Score).ToList();
	}

	public async Task<ActionResult> JoinTeamAsync(int teamId, string userId, string code) {
		var team = await GetTeamAsync(teamId);
		if (team is null) {
			return new BadRequestObjectResult("Invalid team id");
		}
		
		if (team.JoinCode != code) {
			return new BadRequestObjectResult("Invalid join code");
		}
		
		if (team.Members.Count >= team.Event.MaxTeamMembers && team.Event.MaxTeamMembers != -1) {
			return new BadRequestObjectResult("Team is full");
		}
		
		if (team.Event.JoinUntilTime < DateTimeOffset.UtcNow) {
			return new BadRequestObjectResult("Event join time has expired");
		}

		var member = await eventService.GetEventMemberAsync(userId, team.EventId);
		if (member is null) {
			return new BadRequestObjectResult("You are not a member of this event");
		}

		if (member.TeamId is not null) {
			return new BadRequestObjectResult("You are already in a team");
		}

		member.TeamId = teamId;
		context.Entry(member).State = EntityState.Modified;
		
		await context.SaveChangesAsync();

		return new OkResult();
	}

	public async Task<ActionResult> LeaveTeamAsync(int teamId, string userId) {
		var team = await GetTeamAsync(teamId);
		if (team is null) {
			return new BadRequestObjectResult("Invalid team id");
		}
		
		if (team.Event.JoinUntilTime < DateTimeOffset.UtcNow) {
			return new BadRequestObjectResult("Event join time has expired");
		}
		
		var member = await eventService.GetEventMemberAsync(userId, team.EventId);
		if (member?.TeamId != teamId) {
			return new BadRequestObjectResult("You are not in this team");
		}

		member.TeamId = null;
		context.Entry(member).State = EntityState.Modified;
		
		if (team.UserId == userId) {
			if (team.Members.Count > 1) {
				return new BadRequestObjectResult("You cannot leave the team as the owner with members still in it");
			}
			
			context.EventTeams.Remove(team);
		}
		
		await context.SaveChangesAsync();

		return new OkResult();
	}

	public async Task<ActionResult> KickMemberValidateAsync(int teamId, string requester, string playerUuidOrIgn) {
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
		
		if (requesterMember.UserId.ToString() != team.UserId) {
			return new BadRequestObjectResult("You are not the team owner");
		}
		
		var member = await eventService.GetEventMemberAsync(playerUuidOrIgn, team.EventId);
		if (member?.TeamId != teamId) {
			return new BadRequestObjectResult("This player is not in this team");
		}
		
		if (member.UserId.ToString() == team.UserId) {
			return new BadRequestObjectResult("You cannot kick the team owner");
		}

		if (team.Members.Count <= 1) {
			context.EventTeams.Remove(team);
		}
		
		member.TeamId = null;
		context.Entry(member).State = EntityState.Modified;
		
		await context.SaveChangesAsync();
		
		return new OkResult();
	}

	public async Task<ActionResult> KickMemberAsync(int teamId, string playerUuidOrIgn) {
		var team = await GetTeamAsync(teamId);
		if (team is null) {
			return new BadRequestObjectResult("Invalid team id");
		}
		
		var member = await eventService.GetEventMemberAsync(playerUuidOrIgn, team.EventId);
		if (member?.TeamId != teamId) {
			return new BadRequestObjectResult("This player is not in this team");
		}
		
		if (team.Members.Count <= 1) {
			context.EventTeams.Remove(team);
		}

		member.TeamId = null;
		context.Entry(member).State = EntityState.Modified;
		
		await context.SaveChangesAsync();
		
		return new OkResult();
	}

	public async Task<ActionResult> UpdateTeamAsync(int id, UpdateEventTeamDto team, string userId) {
		var existing = await GetTeamAsync(id);
		if (existing is null) {
			return new BadRequestObjectResult("Invalid team");
		}
		
		if (existing.Event.JoinUntilTime < DateTimeOffset.UtcNow) {
			return new BadRequestObjectResult("Event has already started");
		}
		
		var member = await eventService.GetEventMemberAsync(userId, existing.EventId);
		if (member?.TeamId is null || member.TeamId != id) {
			return new BadRequestObjectResult("You are not a member of this event");
		}
		
		if (existing.UserId != member.UserId.ToString()) {
			return new BadRequestObjectResult("You are not the team owner");
		}
		
		if (!IsValidTeamName(team.Name)) {
			team.Name = null;
		}
		
		existing.Name = team.Name ?? existing.Name;
		existing.Color = team.Color ?? existing.Color;
		
		context.Entry(existing).State = EntityState.Modified;
		await context.SaveChangesAsync();

		return new OkResult();
	}
	
	public EventTeamsWordListDto GetEventTeamNameWords() {
		return config.Value.TeamsWordList;
	}

	public bool IsValidTeamName([NotNullWhen(true)] string? name) {
		if (string.IsNullOrWhiteSpace(name)) {
			return false;
		}
		
		var words = name.Split(' ');
		if (words.Length is > 3 or < 2) {
			return false;
		}
		
		return words.All(w => _eventTeamWordList.Contains(w));
	}

	public string GetRandomTeamName() {
		var random = new Random();
		var adjective = _eventTeamWordList[random.Next(_eventTeamWordList.Count)];
		var noun = _eventTeamWordList[random.Next(_eventTeamWordList.Count)];
		var verb = _eventTeamWordList[random.Next(_eventTeamWordList.Count)];
		return $"{adjective} {noun} {verb}";
	}
}