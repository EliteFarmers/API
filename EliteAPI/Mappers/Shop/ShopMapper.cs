using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing.Shop;
using EliteAPI.Models.Entities.Monetization;

namespace EliteAPI.Mappers.Shop;

public class ShopMapper : Profile {
	public ShopMapper() {
		CreateMap<Category, ShopCategoryDto>();
		CreateMap<Category, ShopCategoryWithProductsDto>();
	}
}