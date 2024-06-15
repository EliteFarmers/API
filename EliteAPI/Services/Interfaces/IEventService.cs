using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.Interfaces;

public interface IEventService {
	public Task<List<EventDetailsDto>> GetUpcomingEvents();
	public Task<ActionResult<Event>> CreateEvent(CreateEventDto eventDto, ulong guildId);
	public Task<ActionResult<EventMember>> CreateEventMember(Event @event, CreateEventMemberDto eventMemberDto);
	public Task<EventMember?> GetEventMemberAsync(string playerUuidOrIgn, ulong eventId);
	
	/// <summary>
	/// Initializes the event member with default values if needed
	/// </summary>
	/// <param name="eventMember"></param>
	/// <returns></returns>
	public Task InitializeEventMember(EventMember eventMember, ProfileMember member);
}