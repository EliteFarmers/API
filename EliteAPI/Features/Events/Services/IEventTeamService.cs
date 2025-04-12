using EliteAPI.Configuration.Settings;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Events.Services;

public interface IEventTeamService {
	Task<ActionResult> CreateUserTeamAsync(ulong eventId, CreateEventTeamDto team, string userId);
	Task<ActionResult> CreateAdminTeamAsync(ulong eventId, CreateEventTeamDto team, string userId);
	Task<ActionResult> UpdateTeamAsync(int id, UpdateEventTeamDto team, string userId, bool admin = false);
	Task<ActionResult> RegenerateJoinCodeAsync(int teamId, string userId);
	Task<ActionResult> DeleteTeamValidateAsync(int id);
	Task<ActionResult> DeleteTeamAsync(int id);

	Task<EventTeam?> GetTeamAsync(int id);
	Task<EventTeam?> GetTeamAsync(string userId, ulong eventId);
	Task<List<EventTeam>> GetEventTeamsAsync(ulong eventId);
	Task<ActionResult> JoinTeamValidateAsync(int teamId, string userId, string code);
	Task<ActionResult> AddMemberToTeamAsync(int teamId, string playerUuidOrIgn);
	Task<ActionResult> LeaveTeamAsync(int teamId, string userId);
	Task<ActionResult> KickMemberValidateAsync(int teamId, string requester, string playerUuidOrIgn);
	Task<ActionResult> KickMemberAsync(int teamId, string playerUuidOrIgn);
	Task<ActionResult> SetTeamOwnerValidateAsync(int teamId, string userId, string playerUuidOrIgn);
	Task<ActionResult> SetTeamOwnerAsync(int teamId, string playerUuidOrIgn);
	
	EventTeamsWordListDto GetEventTeamNameWords();
	bool IsValidTeamName(List<string>? name);
}