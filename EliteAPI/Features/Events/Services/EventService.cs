using System.Diagnostics.CodeAnalysis;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Events;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IMapper = AutoMapper.IMapper;

namespace EliteAPI.Features.Events.Services;

[RegisterService<IEventService>(LifeTime.Scoped)]
public class EventService(
	DataContext context,
	IMojangService mojangService,
	IMapper mapper) 
	: IEventService 
{
	public async Task<List<EventDetailsDto>> GetUpcomingEvents() {
		var events = await context.Events
            .Where(e => e.EndTime > DateTimeOffset.UtcNow && e.Approved)
            .OrderBy(e => e.StartTime)
            .AsNoTracking()
            .ToListAsync();
        
        return mapper.Map<List<EventDetailsDto>>(events);
	}

	public async Task<ActionResult<Event>> CreateEvent(CreateEventDto eventDto, ulong guildId) {
		var guild = context.Guilds
			.FirstOrDefault(g => g.Id == guildId);
		
		if (guild is null) {
			return new BadRequestObjectResult("Guild not found");
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

		if (eventDto.MaxTeams < -1) {
			return new BadRequestObjectResult("Invalid max teams");
		}
		
		if (eventDto.MaxTeamMembers < -1) {
			return new BadRequestObjectResult("Invalid max members");
		}
		
		if (eventDto is { MaxTeams: -1, MaxTeamMembers: -1 }) {
			return new BadRequestObjectResult("Max members cannot be unlimited if max teams is unlimited");
		}

		var eliteEvent = eventDto.Type switch {
			EventType.FarmingWeight => CreateWeightEvent(eventDto, guildId),
			EventType.Medals => CreateMedalsEvent(eventDto, guildId),
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

	public async Task<ActionResult<EventMember>> CreateEventMember(Event eliteEvent, CreateEventMemberDto eventMemberDto) {
		switch (eliteEvent) {
			case WeightEvent weightEvent:
				return await CreateWeightEventMember(weightEvent, eventMemberDto);
			case MedalEvent medalEvent:
				return await CreateMedalsEventMember(medalEvent, eventMemberDto);
		}
		return new BadRequestObjectResult("Invalid event type");
	}

	public async Task<EventMember?> GetEventMemberByIdAsync(string userId, ulong eventId) {
		if (!ulong.TryParse(userId, out var userIdLong)) return null;
		
		var member = await context.EventMembers.AsNoTracking()
			.FirstOrDefaultAsync(m => m.UserId == userIdLong && m.EventId == eventId);

		return member;
	}

	public async Task<EventMember?> GetEventMemberAsync(string playerUuidOrIgn, ulong eventId) {
		var uuid = await mojangService.GetUuid(playerUuidOrIgn);

		var userId = await context.MinecraftAccounts
			.Where(m => m.Id == uuid)
			.Select(m => m.AccountId)
			.FirstOrDefaultAsync();
	
		var stringId = userId?.ToString();
		if (stringId is null) return null;
		
		return await GetEventMemberByIdAsync(stringId, eventId);
	}

	public bool CanCreateEvent(Guild guild, [NotNullWhen(false)] out string? reason) {
		if (!guild.Features.EventsEnabled) {
			reason = "This guild does not have access to make events!";
			return false;
		}
        
		if (guild.Features.EventSettings is not null) {
			// Check if the guild has reached their max amount of events for the month
			var startOfMonth = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
			var count = guild.Features.EventSettings.CreatedEvents.Count(e => e.CreatedAt > startOfMonth);
			if (count >= guild.Features.EventSettings.MaxMonthlyEvents) {
				reason = "You have reached your maximum amount of events for this month!";
				return false;
			}
		}

		reason = null;
		return true;
	}

	public async Task InitializeEventMember(EventMember eventMember, ProfileMember member) {
		// Don't do anything if the data isn't past the event start time
		if (DateTimeOffset.FromUnixTimeSeconds(member.LastUpdated) < eventMember.StartTime) return;
		
		// Only weight event has an initialization step so far
		if (eventMember is WeightEventMember weightEventMember) {
			weightEventMember.Initialize(member);
			await context.SaveChangesAsync();
		}
	}

	private async Task<ActionResult<EventMember>> CreateWeightEventMember(Event weightEvent, CreateEventMemberDto eventMemberDto) {
		var member = new WeightEventMember {
			EventId = weightEvent.Id,
			ProfileMemberId = eventMemberDto.ProfileMemberId,
			UserId = eventMemberDto.UserId,
			
			Status = weightEvent.Active ? EventMemberStatus.Active : EventMemberStatus.Inactive,
			Score = eventMemberDto.Score,
			
			LastUpdated = DateTimeOffset.UtcNow,
			StartTime = eventMemberDto.StartTime,
			EndTime = eventMemberDto.EndTime
		}; 
		
		AddEventMember(member);
		await InitializeEventMember(member, eventMemberDto.ProfileMember);
		await context.SaveChangesAsync();
		
		return member;
	}
	
	private async Task<ActionResult<EventMember>> CreateMedalsEventMember(Event medalEvent, CreateEventMemberDto eventMemberDto) {
		var member = new MedalEventMember {
			EventId = medalEvent.Id,
			ProfileMemberId = eventMemberDto.ProfileMemberId,
			UserId = eventMemberDto.UserId,
			
			Status = medalEvent.Active ? EventMemberStatus.Active : EventMemberStatus.Inactive,
			Score = eventMemberDto.Score,
			
			LastUpdated = DateTimeOffset.UtcNow,
			StartTime = eventMemberDto.StartTime,
			EndTime = eventMemberDto.EndTime
		};
		
		AddEventMember(member);
		await InitializeEventMember(member, eventMemberDto.ProfileMember);
		await context.SaveChangesAsync();
		
		return member;
	}
	
	private ActionResult<Event> CreateWeightEvent(CreateEventDto eventDto, ulong guildId) {
		var startTime = eventDto.StartTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.StartTime.Value)
			: (DateTimeOffset?) null;
		var endTime = eventDto.EndTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.EndTime.Value) 
			: (DateTimeOffset?) null;
		var joinUntilTime = eventDto.JoinTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.JoinTime.Value) 
			: endTime;
		
		if (endTime is null || startTime is null || joinUntilTime is null) {
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

			StartTime = startTime.Value,
			EndTime = endTime.Value,
			JoinUntilTime = joinUntilTime.Value,
			
			DynamicStartTime = eventDto.DynamicStartTime ?? false,
			Active = false,
			
			RequiredRole = eventDto.RequiredRole,
			BlockedRole = eventDto.BlockedRole,
			
			MaxTeams = eventDto.MaxTeams,
			MaxTeamMembers = eventDto.MaxTeamMembers,
			
			GuildId = guildId,
		};

		if (!PopulateEventData(eliteEvent, eventDto)) {
			return new BadRequestObjectResult("Invalid event data");
		}
		AddEvent(eliteEvent);
		
		return eliteEvent;
	} 
	
	private ActionResult<Event> CreateMedalsEvent(CreateEventDto eventDto, ulong guildId) {
		var startTime = eventDto.StartTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.StartTime.Value)
			: (DateTimeOffset?) null;
		var endTime = eventDto.EndTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.EndTime.Value) 
			: (DateTimeOffset?) null;
		var joinUntilTime = eventDto.JoinTime is not null 
			? DateTimeOffset.FromUnixTimeSeconds(eventDto.JoinTime.Value) 
			: endTime;
		
		if (endTime is null || startTime is null || joinUntilTime is null) {
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
	
			StartTime = startTime.Value,
			EndTime = endTime.Value,
			JoinUntilTime = joinUntilTime.Value,
			
			DynamicStartTime = eventDto.DynamicStartTime ?? false,
			Active = false,
			
			RequiredRole = eventDto.RequiredRole,
			BlockedRole = eventDto.BlockedRole,
			
			MaxTeams = eventDto.MaxTeams,
			MaxTeamMembers = eventDto.MaxTeamMembers,
			
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
				if (@event is WeightEvent weightEvent) {
                    weightEvent.Data = (eventDto is CreateWeightEventDto weightDto ? weightDto.Data : null) ?? weightEvent.Data;
                }
				break;
			case EventType.Medals:
				if (@event is MedalEvent medalEvent) {
                    medalEvent.Data = (eventDto is CreateMedalEventDto medalDto ? medalDto.Data : null) ?? medalEvent.Data;
                }
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
	
	private void AddEventMember(EventMember member) {
		switch (member) {
			case WeightEventMember weightEvent:
				context.WeightEventMembers.Add(weightEvent);
				break;
			case MedalEventMember medalEvent:
				context.MedalEventMembers.Add(medalEvent);
				break;
			default:
				context.EventMembers.Add(member);
				break;
		}
	}
}