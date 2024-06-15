using Asp.Versioning;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Controllers.Events;

[ApiController, ApiVersion(1.0)]
[Route("[controller]/{eventId}")]
[Route("/v{version:apiVersion}/[controller]/{eventId}")]
public class EventTeamController(
	DataContext context,
	IMapper mapper,
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
	/// Create a team in an event
	/// </summary>
	/// <param name="eventId"></param>
	/// <param name="team"></param>
	/// <returns></returns>
	[HttpPost("teams")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> CreateTeam(ulong eventId, CreateEventTeamDto team) {
		team.EventId = eventId.ToString();
		
		var result = await teamService.CreateTeamAsync(team);

		return result.Result switch {
			BadRequestObjectResult => BadRequest(result.Value),
			_ => Ok()
		};
	}
	
	
	
	
}