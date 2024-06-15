using System.Security.Claims;
using Asp.Versioning;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Controllers.Events;

[ApiController, ApiVersion(1.0)]
[Route("[controller]/{eventId}")]
[Route("/v{version:apiVersion}/[controller]/{eventId}")]
public class EventTeamController(
	DataContext context,
	IMapper mapper,
	UserManager<ApiUser> userManager,
	IEventTeamService teamService)
	: ControllerBase 
{
	/// <summary>
	/// Get all teams in an event
	/// </summary>
	/// <param name="eventId"></param>
	/// <returns></returns>
	[HttpGet("teams")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<List<EventTeamWithMembersDto>>> GetEventTeams(ulong eventId) {
		var teams = await teamService.GetEventTeamsAsync(eventId);
		return mapper.Map<List<EventTeamWithMembersDto>>(teams);
	}
	
	/// <summary>
	/// Get one team in an event
	/// </summary>
	/// <param name="eventId"></param>
	/// <returns></returns>
	[HttpGet("team/{teamId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<ActionResult<EventTeamWithMembersDto>> GetEventTeams(ulong eventId, int teamId) {
		var team = await teamService.GetTeamAsync(teamId);
		if (team is null) {
			return NotFound("Team not found");
		}
		
		return mapper.Map<EventTeamWithMembersDto>(team);
	}
	
	/// <summary>
	/// Create a team in an event
	/// </summary>
	/// <param name="eventId"></param>
	/// <param name="team"></param>
	/// <returns></returns>
	[Authorize]
	[HttpPost("teams")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> CreateTeam(ulong eventId, CreateEventTeamDto team) {
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) {
			return Unauthorized();
		}
		
		team.EventId = eventId.ToString();
		
		var result = await teamService.CreateTeamAsync(team, userId);

		return result.Result switch {
			BadRequestObjectResult => BadRequest(result.Value),
			_ => Ok()
		};
	}

	/// <summary>
	/// Edit a team
	/// </summary>
	/// <param name="eventId"></param>
	/// <param name="teamId"></param>
	/// <param name="team"></param>
	/// <returns></returns>
	[Authorize]
	[HttpPatch("team/{teamId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> UpdateTeam(ulong eventId, int teamId, UpdateEventTeamDto team) {
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) {
			return Unauthorized();
		}
		
		team.EventId = eventId.ToString();
		
		return await teamService.UpdateTeamAsync(teamId, team, userId);
	}
	
	/// <summary>
	/// Delete a team
	/// </summary>
	/// <param name="eventId"></param>
	/// <param name="teamId"></param>
	/// <returns></returns>
	[Authorize]
	[HttpDelete("team/{teamId:int}")]
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
		
		if (team.OwnerId.ToString() != userId) {
			return Unauthorized("You are not the team owner");
		}
		
		return await teamService.DeleteTeamAsync(teamId);
	}

	/// <summary>
	/// Join a team
	/// </summary>
	/// <param name="eventId"></param>
	/// <param name="teamId"></param>
	/// <param name="joinCode"></param>
	/// <returns></returns>
	[Authorize]
	[HttpPost("team/{teamId:int}/join")]
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
	[HttpPost("team/{teamId:int}/leave")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> JoinTeam(ulong eventId, int teamId) {
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) {
			return Unauthorized();
		}
		
		return await teamService.LeaveTeamAsync(teamId, userId);
	}
	
}