using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Services.EventService;

public interface IEventService {
	public Task<List<EventDetailsDto>> GetUpcomingEvents();
}