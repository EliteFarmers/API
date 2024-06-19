using System.Security.Claims;
using System.Text.Json;
using Asp.Versioning;
using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace EliteAPI.Controllers.Events;

[ApiController, ApiVersion(1.0)]
[Route("event")]
[Route("/v{version:apiVersion}/event")]
public class EventTeamController(
	DataContext context,
	IMapper mapper,
	IConnectionMultiplexer redis,
	IEventTeamService teamService)
	: ControllerBase 
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};
	
	/// <summary>
	/// Get all teams in an event
	/// </summary>
	/// <param name="eventId"></param>
	/// <returns></returns>
	[HttpGet("{eventId}/teams")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<List<EventTeamWithMembersDto>>> GetEventTeams(ulong eventId) {
		var db = redis.GetDatabase();
		var key = $"event:{eventId}:teams";
		var cached = await db.StringGetAsync(key);
        
		if (cached is { IsNullOrEmpty: false, HasValue: true }) {
			var cachedTeams = JsonSerializer.Deserialize<List<EventTeamWithMembersDto>>(cached!, JsonOptions);
			return Ok(cachedTeams);
		}

		var eliteEvent = await context.Events.FindAsync(eventId);
		
		var teams = await teamService.GetEventTeamsAsync(eventId);
		var mapped = mapper.Map<List<EventTeamWithMembersDto>>(teams);
        
		var expiry = eliteEvent?.Active is true ? TimeSpan.FromMinutes(2) : TimeSpan.FromMinutes(10);
		await db.StringSetAsync(key, JsonSerializer.Serialize(mapped, JsonOptions), expiry);
        
		return Ok(mapped);
	}
	
	
	/// <summary>
	/// Get one team in an event
	/// </summary>
	/// <param name="eventId"></param>
	/// <returns></returns>
	[OptionalAuthorize]
	[HttpGet("{eventId}/team/{teamId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<ActionResult<EventTeamWithMembersDto>> GetEventTeam(ulong eventId, int teamId) {
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		var isAdmin = User.IsInRole(ApiUserPolicies.Admin);
		
		var team = await teamService.GetTeamAsync(teamId);
		if (team is null) {
			return NotFound("Team not found");
		}
		
		var mapped = mapper.Map<EventTeamWithMembersDto>(team);
		
		if (userId is not null && (team.UserId == userId || isAdmin)) {
			// If the user is the owner of the team, return the join code
			mapped.JoinCode = team.JoinCode;
			return mapped;
		}

		return mapped;
	}
	
	/// <summary>
	/// Create a team in an event
	/// </summary>
	/// <param name="eventId"></param>
	/// <param name="team"></param>
	/// <returns></returns>
	[Authorize]
	[HttpPost("{eventId}/teams")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> CreateTeam(ulong eventId, CreateEventTeamDto team) {
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) {
			return Unauthorized();
		}
		
		return await teamService.CreateTeamAsync(eventId, team, userId);
	}

	/// <summary>
	/// Edit a team
	/// </summary>
	/// <param name="eventId"></param>
	/// <param name="teamId"></param>
	/// <param name="team"></param>
	/// <returns></returns>
	[Authorize]
	[HttpPatch("{eventId}/team/{teamId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> UpdateTeam(ulong eventId, int teamId, UpdateEventTeamDto team) {
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) {
			return Unauthorized();
		}

		return await teamService.UpdateTeamAsync(teamId, team, userId);
	}
	
	/// <summary>
	/// Delete a team
	/// </summary>
	/// <param name="eventId"></param>
	/// <param name="teamId"></param>
	/// <returns></returns>
	[Authorize]
	[HttpDelete("{eventId}/team/{teamId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> DeleteTeam(ulong eventId, int teamId) {
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) {
			return Unauthorized();
		}
		
		var team = await teamService.GetTeamAsync(teamId);
		if (team is null) {
			return BadRequest("Invalid team id");
		}
		
		if (team.UserId != userId) {
			return Unauthorized("You are not the team owner");
		}
		
		return await teamService.DeleteTeamValidateAsync(teamId);
	}

	/// <summary>
	/// Join a team
	/// </summary>
	/// <param name="eventId"></param>
	/// <param name="teamId"></param>
	/// <param name="joinCode"></param>
	/// <returns></returns>
	[Authorize]
	[HttpPost("{eventId}/team/{teamId:int}/join")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> JoinTeam(ulong eventId, int teamId, [FromBody] string joinCode) {
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) {
			return Unauthorized();
		}
		
		return await teamService.JoinTeamAsync(teamId, userId, joinCode);
	}
	
	/// <summary>
	/// Leave a team
	/// </summary>
	/// <param name="eventId"></param>
	/// <param name="teamId"></param>
	/// <returns></returns>
	[Authorize]
	[HttpPost("{eventId}/team/{teamId:int}/leave")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> JoinTeam(ulong eventId, int teamId) {
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) {
			return Unauthorized();
		}
		
		return await teamService.LeaveTeamAsync(teamId, userId);
	}
	
	/// <summary>
	/// Get team name words
	/// </summary>
	/// <returns></returns>
	[HttpGet("teams/words")]
	[ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Any)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public ActionResult<EventTeamsWordListDto> GetEventTeamWordList() {
		return teamService.GetEventTeamNameWords();
	}
}