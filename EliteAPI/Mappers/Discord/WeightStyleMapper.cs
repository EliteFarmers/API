using AutoMapper;
using EliteAPI.Features.Images.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Monetization;

namespace EliteAPI.Mappers.Discord;

public class WeightStyleMapper : Profile {
	public WeightStyleMapper() {
		CreateMap<WeightStyle, WeightStyleLinkedDto>();

		CreateMap<WeightStyle, WeightStyleDto>()
			.ForMember(w => w.Image, opt => opt.MapFrom(w => w.Image))
			.ForMember(w => w.Images, opt => opt.MapFrom(w => w.Images))
			.ForMember(w => w.Products, opt => opt.MapFrom(w => w.Products));

		CreateMap<WeightStyle, WeightStyleWithDataDto>()
			.ForMember(w => w.Data, opt => opt.MapFrom(w => w.Data))
			.ForMember(w => w.Leaderboard, opt => opt.MapFrom(w => w.Leaderboard))
			.ForMember(w => w.Image, opt => opt.MapFrom(w => w.Image))
			.ForMember(w => w.Images, opt => opt.MapFrom(w => w.Images))
			.ForMember(w => w.Products, opt => opt.MapFrom(w => w.Products));

		CreateMap<Image, ImageAttachmentDto>()
			.ConvertUsing(image => image.ToDto()!);

		CreateMap<WeightStyleData, WeightStyleDataDto>()
			.ForMember(w => w.Decal, opt => opt.MapFrom(w => w.Decal))
			.ForMember(w => w.Elements, opt => opt.MapFrom(w => w.Elements))
			.ReverseMap();

		CreateMap<WeightStyleDecal, WeightStyleDecalDto>()
			.ForMember(w => w.Crops, opt => opt.MapFrom(w => w.Crops))
			.ForMember(w => w.Start, opt => opt.MapFrom(w => w.Start))
			.ForMember(w => w.End, opt => opt.MapFrom(w => w.End))
			.ReverseMap();

		CreateMap<WeightStyleElements, WeightStyleElementsDto>()
			.ForMember(w => w.Background, opt => opt.MapFrom(w => w.Background))
			.ForMember(w => w.Gradients, opt => opt.MapFrom(w => w.Gradients))
			.ForMember(w => w.Name, opt => opt.MapFrom(w => w.Name))
			.ForMember(w => w.Weight, opt => opt.MapFrom(w => w.Weight))
			.ForMember(w => w.Label, opt => opt.MapFrom(w => w.Label))
			.ForMember(w => w.Head, opt => opt.MapFrom(w => w.Head))
			.ForMember(w => w.Badge, opt => opt.MapFrom(w => w.Badge))
			.ForMember(w => w.Rank, opt => opt.MapFrom(w => w.Rank))
			.ForMember(w => w.RankWithBadge, opt => opt.MapFrom(w => w.RankWithBadge))
			.ReverseMap();

		CreateMap<LeaderboardStyleData, LeaderboardStyleDataDto>()
			.ForMember(w => w.Background, opt => opt.MapFrom(w => w.Background))
			.ForMember(w => w.Overlay, opt => opt.MapFrom(w => w.Overlay))
			.ForMember(w => w.Name, opt => opt.MapFrom(w => w.Name))
			.ForMember(w => w.Rank, opt => opt.MapFrom(w => w.Rank))
			.ForMember(w => w.Score, opt => opt.MapFrom(w => w.Score))
			.ForMember(w => w.Subtitle, opt => opt.MapFrom(w => w.Subtitle))
			.ReverseMap();

		CreateMap<LeaderboardStyleLayer, LeaderboardStyleLayerDto>()
			.ReverseMap();

		CreateMap<LeaderboardStyleText, LeaderboardStyleTextDto>()
			.ReverseMap();

		CreateMap<WeightStyleBackground, WeightStyleBackgroundDto>()
			.ForMember(w => w.Size, opt => opt.MapFrom(w => w.Size))
			.ForMember(w => w.Rects, opt => opt.MapFrom(w => w.Rects))
			.ReverseMap();

		CreateMap<WeightStyleGradient, WeightStyleGradientDto>()
			.ForMember(w => w.Bounds, opt => opt.MapFrom(w => w.Bounds))
			.ForMember(w => w.Direction, opt => opt.MapFrom(w => w.Direction))
			.ForMember(w => w.Stops, opt => opt.MapFrom(w => w.Stops))
			.ReverseMap();

		CreateMap<WeightStyleDirection, WeightStyleDirectionDto>().ReverseMap();

		CreateMap<WeightStyleGradientStop, WeightStyleGradientStopDto>().ReverseMap();

		CreateMap<WeightStyleElement, WeightStyleElementDto>()
			.ForMember(w => w.Background, opt => opt.MapFrom(w => w.Background))
			.ForMember(w => w.Position, opt => opt.MapFrom(w => w.Position))
			.ForMember(w => w.Outline, opt => opt.MapFrom(w => w.Outline))
			.ReverseMap();

		CreateMap<WeightStyleTextBackground, WeightStyleTextBackgroundDto>().ReverseMap();

		CreateMap<WeightStyleElementOutline, WeightStyleElementOutlineDto>().ReverseMap();

		CreateMap<WeightStyleBackgroundRect, WeightStyleBackgroundRectDto>()
			.ForMember(w => w.Start, opt => opt.MapFrom(w => w.Start))
			.ForMember(w => w.End, opt => opt.MapFrom(w => w.End))
			.ReverseMap();

		CreateMap<WeightStylePosition, WeightStylePositionDto>().ReverseMap();
	}
}