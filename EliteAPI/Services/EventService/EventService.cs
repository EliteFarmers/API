using System.Text.Json;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using Microsoft.AspNetCore.Mvc;
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

	public async Task<ActionResult<Event>> CreateEvent(CreateEventDto eventDto, ulong guildId, ulong ownerId) {
		var guild = context.Guilds
			.FirstOrDefault(g => g.Id == guildId);
		
		if (guild is null) {
			return new NotFoundObjectResult("Guild not found");
		}
		
		var startTime = eventDto.StartTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.StartTime.Value)
			: (DateTimeOffset?) null;
		var endTime = eventDto.EndTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.EndTime.Value) 
			: (DateTimeOffset?) null;
		
		if (endTime is null || startTime is null) {
			return new BadRequestObjectResult("Invalid start or end time");
		}
		
		if (startTime > endTime) {
			return new BadRequestObjectResult("Start time cannot be after end time");
		}
		
		if (startTime < DateTimeOffset.UtcNow) {
			return new BadRequestObjectResult("Start time cannot be in the past");
		}
		
		if (startTime > DateTimeOffset.UtcNow.AddMonths(2)) {
			return new BadRequestObjectResult("Start time cannot be more than 2 months in the future");
		}
		
		if (endTime.Value.Subtract(startTime.Value).TotalDays < 3) {
			return new BadRequestObjectResult("Event must be at least 3 days long");
		}

		var eliteEvent = eventDto.Type switch {
			EventType.FarmingWeight => CreateWeightEvent(eventDto, guildId, ownerId),
			EventType.Medals => CreateMedalsEvent(eventDto, guildId, ownerId),
			_ => null
		};

		if (eliteEvent is null) {
			return new BadRequestObjectResult("Invalid event type");
		}
		
		if (eliteEvent.Value is null) {
			return eliteEvent;
		}

		guild.Features.EventSettings ??= new GuildEventSettings();
		guild.Features.EventSettings.CreatedEvents.Add(new EventCreatedDto {
			Id = eliteEvent.Value.Id.ToString(),
			CreatedAt = DateTimeOffset.UtcNow
		});
		context.Guilds.Update(guild);
        
		await context.SaveChangesAsync();

		return eliteEvent;
	}
	
	private ActionResult<Event> CreateWeightEvent(CreateEventDto eventDto, ulong guildId, ulong ownerId) {
		var startTime = eventDto.StartTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.StartTime.Value)
			: (DateTimeOffset?) null;
		var endTime = eventDto.EndTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.EndTime.Value) 
			: (DateTimeOffset?) null;
		
		if (endTime is null || startTime is null) {
			return new BadRequestObjectResult("Invalid start or end time");
		}
		
		var eliteEvent = new WeightEvent {
			Id = guildId + (ulong) new Random().Next(100000000, 999999999),
			Type = EventType.FarmingWeight,
			
			Name = eventDto.Name ?? "Untitled Event",
			Description = eventDto.Description,
			Rules = eventDto.Rules,
			PrizeInfo = eventDto.PrizeInfo,
			Public = true, // For now, all events are public
			
			Banner = eventDto.Banner,
			Thumbnail = eventDto.Thumbnail,
			
			StartTime = startTime.Value,
			EndTime = endTime.Value,
			DynamicStartTime = eventDto.DynamicStartTime ?? false,
			Active = false,
			
			RequiredRole = eventDto.RequiredRole,
			BlockedRole = eventDto.BlockedRole,
			
			OwnerId = ownerId,
			GuildId = guildId,
		};

		if (!PopulateEventData(eliteEvent, eventDto)) {
			return new BadRequestObjectResult("Invalid event data");
		}
		AddEvent(eliteEvent);
		
		return eliteEvent;
	} 
	
	private ActionResult<Event> CreateMedalsEvent(CreateEventDto eventDto, ulong guildId, ulong ownerId) {
		var startTime = eventDto.StartTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.StartTime.Value)
			: (DateTimeOffset?) null;
		var endTime = eventDto.EndTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.EndTime.Value) 
			: (DateTimeOffset?) null;
		
		if (endTime is null || startTime is null) {
			return new BadRequestObjectResult("Invalid start or end time");
		}
		
		var eliteEvent = new MedalEvent {
			Id = guildId + (ulong) new Random().Next(100000000, 999999999),
			Type = EventType.Medals,
			
			Name = eventDto.Name ?? "Untitled Event",
			Description = eventDto.Description,
			Rules = eventDto.Rules,
			PrizeInfo = eventDto.PrizeInfo,
			Public = true, // For now, all events are public
			
			Banner = eventDto.Banner,
			Thumbnail = eventDto.Thumbnail,
			
			StartTime = startTime.Value,
			EndTime = endTime.Value,
			DynamicStartTime = eventDto.DynamicStartTime ?? false,
			Active = false,
			
			RequiredRole = eventDto.RequiredRole,
			BlockedRole = eventDto.BlockedRole,
			
			OwnerId = ownerId,
			GuildId = guildId,
		};

		if (!PopulateEventData(eliteEvent, eventDto)) {
			return new BadRequestObjectResult("Invalid event data");
		}
		AddEvent(eliteEvent);
		
		return eliteEvent;
	}
	
	private static bool PopulateEventData(Event @event, CreateEventDto eventDto) {
		switch (eventDto.Type) {
			case EventType.None:
			case EventType.FarmingWeight:
				if (@event is not WeightEvent weightEvent || eventDto is not CreateWeightEventDto weightDto) return false;
				weightEvent.Data = weightDto.Data ?? weightEvent.Data;
				break;
			case EventType.Medals:
				if (@event is not MedalEvent medalEvent || eventDto is not CreateMedalEventDto medalDto) return false;
				medalEvent.Data = medalDto.Data ?? medalEvent.Data;
				break;
			default:
				return false;
		}
		
		return true;
	}
	
	private void AddEvent(Event @event) {
		switch (@event) {
			case WeightEvent weightEvent:
				context.WeightEvents.Add(weightEvent);
				break;
			case MedalEvent medalEvent:
				context.MedalEvents.Add(medalEvent);
				break;
			default:
				context.Events.Add(@event);
				break;
		}
	}
}