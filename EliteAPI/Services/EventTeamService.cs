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
		ImmutableList.CreateRange(config.Value.TeamsWordList.First
			.Concat(config.Value.TeamsWordList.Second)
			.Concat(config.Value.TeamsWordList.Third));
	
	public async Task<ActionResult> CreateUserTeamAsync(ulong eventId, CreateEventTeamDto team, string userId) {
		var member = await eventService.GetEventMemberByIdAsync(userId, eventId);
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
			return new BadRequestObjectResult("Invalid team name!");
		}
        
		var newName = string.Join(' ', team.Name);
		if (await TeamNameExists(eventId, newName)) {
			return new BadRequestObjectResult("This team name is already in use!");
		}
		
		member.Team = new EventTeam {
			Name = newName,
			Color = team.Color,
			UserId = userId,
			EventId = eventId
		};

		context.EventMembers.Update(member);
		await context.SaveChangesAsync();

		return new OkResult();
	}

	public async Task<ActionResult> CreateAdminTeamAsync(ulong eventId, CreateEventTeamDto team, string userId) {
		var @event = await context.Events.FindAsync(eventId);
		if (@event is null) {
			return new BadRequestObjectResult("Invalid event id");
		}
		
		if (!IsValidTeamName(team.Name)) {
			return new BadRequestObjectResult("Invalid team name!");
		}
		
		var newName = string.Join(' ', team.Name);
		if (await TeamNameExists(eventId, newName)) {
			return new BadRequestObjectResult("This team name is already in use!");
		}

		var newTeam = new EventTeam {
			Name = newName,
			Color = team.Color,
			UserId = "admin",
			EventId = eventId
		};

		context.EventTeams.Add(newTeam);
		await context.SaveChangesAsync();

		return new OkResult();
	}

	public async Task<ActionResult> RegenerateJoinCodeAsync(int teamId, string userId) {
		var team = await GetTeamAsync(teamId);
		if (team is null) {
			return new BadRequestObjectResult("Invalid team id");
		}
		
		if (team.UserId != userId) {
			return new UnauthorizedObjectResult("You are not the team owner");
		}
		
		team.JoinCode = EventTeam.NewJoinCode();
		context.Entry(team).State = EntityState.Modified;
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
		
		context.Entry(team).State = EntityState.Deleted;
		await context.SaveChangesAsync();

		return new OkResult();
	}

	public async Task<ActionResult> DeleteTeamAsync(int id) {
		var team = await GetTeamAsync(id);
		if (team is null) {
			return new BadRequestObjectResult("Invalid team id");
		}
		
		context.Entry(team).State = EntityState.Deleted;
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

	public async Task<ActionResult> JoinTeamValidateAsync(int teamId, string userId, string code) {
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

		var member = await eventService.GetEventMemberByIdAsync(userId, team.EventId);
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

	public async Task<ActionResult> AddMemberToTeamAsync(int teamId, string playerUuidOrIgn) {
		var team = await GetTeamAsync(teamId);
		if (team is null) {
			return new BadRequestObjectResult("Invalid team id");
		}
		
		var member = await eventService.GetEventMemberAsync(playerUuidOrIgn, team.EventId);
		if (member is null) {
			return new BadRequestObjectResult("You are not a member of this event");
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
		
		var member = await eventService.GetEventMemberByIdAsync(userId, team.EventId);
		if (member?.TeamId != teamId) {
			return new BadRequestObjectResult("You are not in this team");
		}
		
		member.TeamId = null;
		member.Team = null;
		context.Entry(member).State = EntityState.Modified;
		
		if (team.UserId == userId) {
			if (team.Members.Count > 1) {
				return new BadRequestObjectResult("You cannot leave the team as the owner with members still in it");
			}
			
			context.Entry(team).State = EntityState.Deleted;
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
		
		var requesterMember = await eventService.GetEventMemberByIdAsync(requester, team.EventId);
		if (requesterMember?.TeamId != teamId) {
			return new BadRequestObjectResult("You are not in this team");
		}
		
		if (requesterMember.UserId.ToString() != team.UserId) {
			return new BadRequestObjectResult("You are not the team owner");
		}
		
		var member = team.Members.FirstOrDefault(m => 
			m.ProfileMember.MinecraftAccount.Id == playerUuidOrIgn 
			|| m.ProfileMember.MinecraftAccount.Name.Equals(playerUuidOrIgn, StringComparison.InvariantCultureIgnoreCase));
		
		if (member is null) {
			return new BadRequestObjectResult("This player is not in this team");
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
		
		var member = await eventService.GetEventMemberByIdAsync(playerUuidOrIgn, team.EventId);
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

	public async Task<ActionResult> UpdateTeamAsync(int id, UpdateEventTeamDto team, string userId, bool admin = false) {
		var existing = await GetTeamAsync(id);
		if (existing is null) {
			return new BadRequestObjectResult("Invalid team");
		}
		
		if (!admin && existing.Event.JoinUntilTime < DateTimeOffset.UtcNow) {
			return new BadRequestObjectResult("Event has already started");
		}
		
		var member = await eventService.GetEventMemberByIdAsync(userId, existing.EventId);
		if (!admin && (member?.TeamId is null || member.TeamId != id)) {
			return new BadRequestObjectResult("You are not a member of this event");
		}
		
		if (!admin && existing.UserId != member?.UserId.ToString()) {
			return new BadRequestObjectResult("You are not the team owner");
		}
		
		if (!IsValidTeamName(team.Name)) {
			team.Name = null;
		}
		
		var newName = team.Name is not null ? string.Join(' ', team.Name) : existing.Name;
		if (newName != existing.Name && await TeamNameExists(existing.EventId, newName)) {
			return new BadRequestObjectResult("This team name is already in use!");
		}

		existing.Name = newName;
		existing.Color = team.Color ?? existing.Color;
		
		context.Entry(existing).State = EntityState.Modified;
		await context.SaveChangesAsync();

		return new OkResult();
	}
	
	private async Task<bool> TeamNameExists(ulong eventId,string name) {
		return await context.EventTeams
			.Where(e => e.EventId == eventId)
			.AnyAsync(e => e.Name == name);
	}
	
	public EventTeamsWordListDto GetEventTeamNameWords() {
		return config.Value.TeamsWordList;
	}

	public bool IsValidTeamName([NotNullWhen(true)] List<string>? words) {
		if (words is null) {
			return false;
		}
		
		var distinctWords = words.Distinct().ToList();
		
		if (distinctWords.Count is > 3 or < 2) {
			return false;
		}
		
		return words.All(w => _eventTeamWordList.Contains(w));
	}

	private List<string> GetRandomTeamName() {
		var random = new Random();
		var first = _eventTeamWordList[random.Next(_eventTeamWordList.Count)];
		var second = _eventTeamWordList[random.Next(_eventTeamWordList.Count)];
		var third = _eventTeamWordList[random.Next(_eventTeamWordList.Count)];
		return [ first, second, third ];
	}
}