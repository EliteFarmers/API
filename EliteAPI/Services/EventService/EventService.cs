using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Services.EventService;

public class EventService(DataContext context, IMapper mapper) : IEventService 
{
	public async Task<List<EventDetailsDto>> GetUpcomingEvents() {
		var events = await context.Events
            .Where(e => e.EndTime > DateTimeOffset.UtcNow)
            .OrderBy(e => e.StartTime)
            .AsNoTracking()
            .ToListAsync();
        
        return mapper.Map<List<EventDetailsDto>>(events);
	}
}