using System.Globalization;
using AutoMapper;
using EliteAPI.Features.Profiles;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;

namespace EliteAPI.Mappers.Events;

public class EventMappers : Profile
{
	public EventMappers() {
		CreateMap<Event, EventDetailsDto>()
			.ForMember(e => e.Id, opt => opt.MapFrom(e => e.Id.ToString()))
			.ForMember(e => e.StartTime, opt => opt.MapFrom(e => e.StartTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.EndTime, opt => opt.MapFrom(e => e.EndTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.JoinUntilTime, opt => opt.MapFrom(e => e.JoinUntilTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.GuildId, opt => opt.MapFrom(e => e.GuildId.ToString()))
			.ForMember(e => e.Banner, opt => opt.MapFrom(e => e.Banner))
			.ForMember(e => e.Mode, opt => opt.MapFrom(e => e.GetMode()));

		CreateMap<WeightEvent, EventDetailsDto>()
			.ForMember(e => e.Id, opt => opt.MapFrom(e => e.Id.ToString()))
			.ForMember(e => e.StartTime, opt => opt.MapFrom(e => e.StartTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.EndTime, opt => opt.MapFrom(e => e.EndTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.JoinUntilTime, opt => opt.MapFrom(e => e.JoinUntilTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.GuildId, opt => opt.MapFrom(e => e.GuildId.ToString()))
			.ForMember(e => e.Banner, opt => opt.MapFrom(e => e.Banner))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data))
			.ForMember(e => e.Mode, opt => opt.MapFrom(e => e.GetMode()));

		CreateMap<MedalEvent, EventDetailsDto>()
			.ForMember(e => e.Id, opt => opt.MapFrom(e => e.Id.ToString()))
			.ForMember(e => e.StartTime, opt => opt.MapFrom(e => e.StartTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.EndTime, opt => opt.MapFrom(e => e.EndTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.JoinUntilTime, opt => opt.MapFrom(e => e.JoinUntilTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.GuildId, opt => opt.MapFrom(e => e.GuildId.ToString()))
			.ForMember(e => e.Banner, opt => opt.MapFrom(e => e.Banner))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data))
			.ForMember(e => e.Mode, opt => opt.MapFrom(e => e.GetMode()));

		CreateMap<PestEvent, EventDetailsDto>()
			.ForMember(e => e.Id, opt => opt.MapFrom(e => e.Id.ToString()))
			.ForMember(e => e.StartTime, opt => opt.MapFrom(e => e.StartTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.EndTime, opt => opt.MapFrom(e => e.EndTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.JoinUntilTime, opt => opt.MapFrom(e => e.JoinUntilTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.GuildId, opt => opt.MapFrom(e => e.GuildId.ToString()))
			.ForMember(e => e.Banner, opt => opt.MapFrom(e => e.Banner))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data))
			.ForMember(e => e.Mode, opt => opt.MapFrom(e => e.GetMode()));

		CreateMap<CollectionEvent, EventDetailsDto>()
			.ForMember(e => e.Id, opt => opt.MapFrom(e => e.Id.ToString()))
			.ForMember(e => e.StartTime, opt => opt.MapFrom(e => e.StartTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.EndTime, opt => opt.MapFrom(e => e.EndTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.JoinUntilTime, opt => opt.MapFrom(e => e.JoinUntilTime.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.GuildId, opt => opt.MapFrom(e => e.GuildId.ToString()))
			.ForMember(e => e.Banner, opt => opt.MapFrom(e => e.Banner))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data))
			.ForMember(e => e.Mode, opt => opt.MapFrom(e => e.GetMode()));
	}
}

public class EventTeamMappers : Profile
{
	public EventTeamMappers() {
		CreateMap<EventTeam, EventTeamDto>()
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.OwnerId, opt => opt.MapFrom(e => e.UserId.ToString()))
			.ForMember(e => e.OwnerUuid, opt => opt.MapFrom(e => e.GetOwnerUuid()))
			.ForMember(e => e.Score,
				opt => opt.MapFrom(e => e.Members.Sum(m => m.Score).ToString(CultureInfo.InvariantCulture)));

		CreateMap<EventTeam, EventTeamWithMembersDto>()
			.ForMember(e => e.JoinCode, opt => opt.Ignore())
			.ForMember(e => e.OwnerId, opt => opt.MapFrom(e => e.UserId.ToString()))
			.ForMember(e => e.OwnerUuid, opt => opt.MapFrom(e => e.GetOwnerUuid()))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.Score,
				opt => opt.MapFrom(e => e.Members.Sum(m => m.Score).ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Members, opt => opt.MapFrom(e => e.Members));
	}
}

public class EventMemberMappers : Profile
{
	public EventMemberMappers() {
		CreateMap<EventMember, EventMemberDto>()
			.ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
			.ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
			.ForMember(e => e.Meta, opt => opt.MapFrom(e => e.ProfileMember.GetCosmeticsDto()))
			.ForMember(e => e.ProfileId, opt => opt.MapFrom(e => e.ProfileMember.ProfileId))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Disqualified, opt => opt.MapFrom(e => e.IsDisqualified))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)));

		CreateMap<WeightEventMember, EventMemberDto>()
			.ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
			.ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
			.ForMember(e => e.Meta, opt => opt.MapFrom(e => e.ProfileMember.GetCosmeticsDto()))
			.ForMember(e => e.ProfileId, opt => opt.MapFrom(e => e.ProfileMember.ProfileId))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Disqualified, opt => opt.MapFrom(e => e.IsDisqualified))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data));

		CreateMap<MedalEventMember, EventMemberDto>()
			.ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
			.ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
			.ForMember(e => e.Meta, opt => opt.MapFrom(e => e.ProfileMember.GetCosmeticsDto()))
			.ForMember(e => e.ProfileId, opt => opt.MapFrom(e => e.ProfileMember.ProfileId))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Disqualified, opt => opt.MapFrom(e => e.IsDisqualified))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data));

		CreateMap<EventMember, EventMemberDetailsDto>()
			.ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
			.ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
			.ForMember(e => e.Meta, opt => opt.MapFrom(e => e.ProfileMember.GetCosmeticsDto()))
			.ForMember(e => e.ProfileId, opt => opt.MapFrom(e => e.ProfileMember.ProfileId))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Disqualified, opt => opt.MapFrom(e => e.IsDisqualified))
			.ForMember(e => e.EstimatedTimeActive,
				opt => opt.MapFrom(e => e.EstimatedTimeActive.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)));

		CreateMap<MedalEventMember, EventMemberDetailsDto>()
			.ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
			.ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
			.ForMember(e => e.Meta, opt => opt.MapFrom(e => e.ProfileMember.GetCosmeticsDto()))
			.ForMember(e => e.ProfileId, opt => opt.MapFrom(e => e.ProfileMember.ProfileId))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Disqualified, opt => opt.MapFrom(e => e.IsDisqualified))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.EstimatedTimeActive,
				opt => opt.MapFrom(e => e.EstimatedTimeActive.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data));

		CreateMap<PestEventMember, EventMemberDetailsDto>()
			.ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
			.ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
			.ForMember(e => e.Meta, opt => opt.MapFrom(e => e.ProfileMember.GetCosmeticsDto()))
			.ForMember(e => e.ProfileId, opt => opt.MapFrom(e => e.ProfileMember.ProfileId))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Disqualified, opt => opt.MapFrom(e => e.IsDisqualified))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.EstimatedTimeActive,
				opt => opt.MapFrom(e => e.EstimatedTimeActive.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data));

		CreateMap<CollectionEventMember, EventMemberDetailsDto>()
			.ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
			.ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
			.ForMember(e => e.Meta, opt => opt.MapFrom(e => e.ProfileMember.GetCosmeticsDto()))
			.ForMember(e => e.ProfileId, opt => opt.MapFrom(e => e.ProfileMember.ProfileId))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Disqualified, opt => opt.MapFrom(e => e.IsDisqualified))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.EstimatedTimeActive,
				opt => opt.MapFrom(e => e.EstimatedTimeActive.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data));

		CreateMap<EventMember, AdminEventMemberDto>()
			.ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
			.ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
			.ForMember(e => e.Meta, opt => opt.MapFrom(e => e.ProfileMember.GetCosmeticsDto()))
			.ForMember(e => e.ProfileId, opt => opt.MapFrom(e => e.ProfileMember.ProfileId))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Disqualified, opt => opt.MapFrom(e => e.IsDisqualified))
			.ForMember(e => e.Notes, opt => opt.MapFrom(e => e.Notes))
			.ForMember(e => e.AccountId, opt => opt.MapFrom(e => e.UserId.ToString()))
			.ForMember(e => e.EstimatedTimeActive,
				opt => opt.MapFrom(e => e.EstimatedTimeActive.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)));

		CreateMap<MedalEventMember, AdminEventMemberDto>()
			.ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
			.ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
			.ForMember(e => e.Meta, opt => opt.MapFrom(e => e.ProfileMember.GetCosmeticsDto()))
			.ForMember(e => e.ProfileId, opt => opt.MapFrom(e => e.ProfileMember.ProfileId))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Disqualified, opt => opt.MapFrom(e => e.IsDisqualified))
			.ForMember(e => e.Notes, opt => opt.MapFrom(e => e.Notes))
			.ForMember(e => e.AccountId, opt => opt.MapFrom(e => e.UserId.ToString()))
			.ForMember(e => e.EstimatedTimeActive,
				opt => opt.MapFrom(e => e.EstimatedTimeActive.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data));

		CreateMap<PestEventMember, AdminEventMemberDto>()
			.ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
			.ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
			.ForMember(e => e.Meta, opt => opt.MapFrom(e => e.ProfileMember.GetCosmeticsDto()))
			.ForMember(e => e.ProfileId, opt => opt.MapFrom(e => e.ProfileMember.ProfileId))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Disqualified, opt => opt.MapFrom(e => e.IsDisqualified))
			.ForMember(e => e.Notes, opt => opt.MapFrom(e => e.Notes))
			.ForMember(e => e.AccountId, opt => opt.MapFrom(e => e.UserId.ToString()))
			.ForMember(e => e.EstimatedTimeActive,
				opt => opt.MapFrom(e => e.EstimatedTimeActive.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data));

		CreateMap<CollectionEventMember, AdminEventMemberDto>()
			.ForMember(e => e.PlayerUuid, opt => opt.MapFrom(e => e.ProfileMember.PlayerUuid))
			.ForMember(e => e.PlayerName, opt => opt.MapFrom(e => e.ProfileMember.MinecraftAccount.Name))
			.ForMember(e => e.Meta, opt => opt.MapFrom(e => e.ProfileMember.GetCosmeticsDto()))
			.ForMember(e => e.ProfileId, opt => opt.MapFrom(e => e.ProfileMember.ProfileId))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.LastUpdated, opt => opt.MapFrom(e => e.LastUpdated.ToUnixTimeSeconds().ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Disqualified, opt => opt.MapFrom(e => e.IsDisqualified))
			.ForMember(e => e.Notes, opt => opt.MapFrom(e => e.Notes))
			.ForMember(e => e.AccountId, opt => opt.MapFrom(e => e.UserId.ToString()))
			.ForMember(e => e.EstimatedTimeActive,
				opt => opt.MapFrom(e => e.EstimatedTimeActive.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)))
			.ForMember(e => e.Data, opt => opt.MapFrom(e => e.Data));

		CreateMap<EventMember, ProfileEventMemberDto>()
			.ForMember(e => e.EventName, opt => opt.MapFrom(e => e.Event.Name))
			.ForMember(e => e.EventId, opt => opt.MapFrom(e => e.EventId.ToString()))
			.ForMember(e => e.TeamId, opt => opt.MapFrom(e => e.TeamId.ToString()))
			.ForMember(e => e.Score, opt => opt.MapFrom(e => e.Score.ToString(CultureInfo.InvariantCulture)));
	}
}