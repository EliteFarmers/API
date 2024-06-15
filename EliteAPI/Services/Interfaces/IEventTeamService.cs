using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.Interfaces;

public interface IEventTeamService {
	Task<ActionResult<EventTeam>> CreateTeamAsync(CreateEventTeamDto team, string userId);
	Task<ActionResult> UpdateTeamAsync(int id, UpdateEventTeamDto team, string userId);
	Task<ActionResult> DeleteTeamAsync(int id);

	Task<EventTeam?> GetTeamAsync(int id);
	Task<EventTeam?> GetTeamAsync(int memberId, ulong eventId);
	Task<EventTeam?> GetTeamAsync(string playerUuidOrIgn, ulong eventId);
	Task<List<EventTeam>> GetEventTeamsAsync(ulong eventId);
	Task<ActionResult> JoinTeamAsync(int teamId, string userId, string code);
	Task<ActionResult> LeaveTeamAsync(int teamId, string userId);
	Task<ActionResult> KickMemberAsync(int teamId, string requester, string playerUuidOrIgn);
}