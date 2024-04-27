using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.EventService;

public interface IEventService {
	public Task<List<EventDetailsDto>> GetUpcomingEvents();
	public Task<ActionResult<Event>> CreateEvent(CreateEventDto eventDto, ulong guildId, ulong ownerId);
}