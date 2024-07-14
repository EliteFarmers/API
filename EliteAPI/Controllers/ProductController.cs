using System.Text.Json;
using Asp.Versioning;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Controllers;

[ApiController, ApiVersion(1.0)]
[Route("/[controller]")]
[Route("/v{version:apiVersion}/[controller]")]
public partial class ProductController(
	DataContext context,
	IAccountService accountService, 
	IMonetizationService monetizationService, 
	IConnectionMultiplexer redis,
	IMapper mapper)
	: ControllerBase
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};
	
	/// <summary>
	/// Get all products
	/// </summary>
	/// <returns></returns>
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<List<ProductDto>> GetProducts()
	{
		const string key = "bot:productlist";
		var db = redis.GetDatabase();
		
		if (db.KeyExists(key)) {
			var products = await db.StringGetAsync(key);
			if (products.HasValue) {
				return JsonSerializer.Deserialize<List<ProductDto>>(products!, JsonOptions) ?? [];
			}
		}
		
		var list = await context.Products
			.Select(x => mapper.Map<ProductDto>(x))
			.ToListAsync();
		
		await db.StringSetAsync(key, JsonSerializer.Serialize(list, JsonOptions), TimeSpan.FromMinutes(5));

		return list;
	}
}