using System.Net.Mime;
using System.Text.Json;
using Asp.Versioning;
using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Monetization;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Controllers;

[ApiController, ApiVersion(1.0)]
[Route("/[controller]")]
[Route("/v{version:apiVersion}/[controller]")]
public class ProductController(
	DataContext context,
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
	[Route("/[controller]s")]
	[Route("/v{version:apiVersion}/[controller]s")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<List<ProductDto>>> GetProducts()
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
	
	/// <summary>
	/// Get a product
	/// </summary>
	/// <returns></returns>
	[HttpGet("{productId}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<ProductDto>> GetProduct(ulong productId)
	{
		var key = $"bot:productlist:{productId}";
		var db = redis.GetDatabase();
		
		if (db.KeyExists(key)) {
			var product = await db.StringGetAsync(key);
			if (product.HasValue) {
				var value = JsonSerializer.Deserialize<ProductDto>(product!, JsonOptions);
				if (value is not null) {
					return value;
				}
			}
		}
		
		var existing = await context.Products
			.Where(p => p.Id == productId)
			.Include(p => p.WeightStyles)
			.Select(x => mapper.Map<ProductDto>(x))
			.FirstOrDefaultAsync();
		
		if (existing is null) {
			return NotFound("Product not found");
		}
		
		await db.StringSetAsync(key, JsonSerializer.Serialize(existing, JsonOptions), TimeSpan.FromMinutes(5));

		return existing;
	}
	
	/// <summary>
	/// Update a product
	/// </summary>
	/// <param name="productId"></param>
	/// <param name="dto"></param>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPatch("{productId}")]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> UpdateProduct(ulong productId, [FromBody] UpdateProductDto dto)
	{
		var product = await context.Products.FindAsync(productId);
		if (product is null) {
			return NotFound("Product not found");
		}
		
		await monetizationService.UpdateProductAsync(productId, dto);
		
		// Clear the product list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:productlist");
		
		return Ok();
	}
	
	/// <summary>
	/// Get weight styles
	/// </summary>
	/// <returns></returns>
	[HttpGet("styles")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<ActionResult<List<WeightStyleDto>>> GetWeightStyles() {
		var existing = await context.WeightStyles
			.Select(s => mapper.Map<WeightStyleDto>(s))
			.ToListAsync();

		return existing;
	}
	
	/// <summary>
	/// Get a weight style
	/// </summary>
	/// <returns></returns>
	[HttpGet("style/{styleId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<ActionResult<WeightStyleDto>> GetWeightStyle(int styleId) {
		var existing = await context.WeightStyles
			.Where(s => s.Id == styleId)
			.Select(s => mapper.Map<WeightStyleDto>(s))
			.FirstOrDefaultAsync();
		
		if (existing is null) {
			return NotFound("Weight style not found");
		}

		return existing;
	}
	
	/// <summary>
	/// Create a weight style
	/// </summary>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPost("style")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> AddWeightStyleImage([FromBody] WeightStyleWithDataDto incoming) {
		if (incoming.Name is null) {
			return BadRequest("Name is required");
		}
		
		var newStyle = new WeightStyle {
			Name = incoming.Name,
			Collection = incoming.Collection,
			Description = incoming.Description
		};
		
		if (incoming.Data is not null) {
			newStyle.Data = mapper.Map<WeightStyleData>(incoming.Data);
		}

		if (incoming.StyleFormatter is not null) {
			newStyle.StyleFormatter = incoming.StyleFormatter;
		}
		
		context.WeightStyles.Add(newStyle);
		await context.SaveChangesAsync();
		
		return Ok();
	}
	
	/// <summary>
	/// Update a weight style
	/// </summary>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPost("style/{styleId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<IActionResult> UpdateWeightStyle(int styleId, [FromBody] WeightStyleWithDataDto incoming) {
		var existing = await context.WeightStyles.FindAsync(styleId);
		if (existing is null) {
			return BadRequest("Weight style not found");
		}
		
		existing.Name = incoming.Name ?? existing.Name;
		existing.Collection = incoming.Collection ?? existing.Collection;
		existing.StyleFormatter = incoming.StyleFormatter ?? existing.StyleFormatter;
		existing.Description = incoming.Description ?? existing.Description;
		existing.Data = incoming.Data is not null ? mapper.Map<WeightStyleData>(incoming.Data) : existing.Data;
		
		context.WeightStyles.Update(existing);
		await context.SaveChangesAsync();
		
		return Ok();
	}
	
	/// <summary>
	/// Delete a weight style
	/// </summary>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpDelete("style/{styleId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<IActionResult> RemoveWeightStyle(int styleId) {
		var existing = await context.WeightStyles.FindAsync(styleId);
		if (existing is null) {
			return BadRequest("Weight style not found");
		}
		
		context.WeightStyles.Remove(existing);
		await context.SaveChangesAsync();
		
		return Ok();
	}
	
	/// <summary>
	/// Add image to a weight style
	/// </summary>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPost("style/{styleId:int}/image")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<IActionResult> AddWeightStyleImage(int styleId, [FromBody] WeightStyleImageDto image)
	{
		var style = await context.WeightStyles.FindAsync(styleId);
		if (style is null) {
			return NotFound("Style not found");
		}

		var newImage = new WeightStyleImage {
			WeightStyleId = styleId,
			Url = image.Url,
			Title = image.Title,
			Description = image.Description,
			Order = image.Order,
		};
		
		context.WeightStyleImages.Add(newImage);
		await context.SaveChangesAsync();
		
		return Ok();
	}
	
	/// <summary>
	/// Remove image from a weight style
	/// </summary>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpDelete("style/{styleId:int}/image/{imageId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<IActionResult> DeleteWeightStyleImage(int styleId, int imageId)
	{
		var image = await context.WeightStyleImages.FindAsync(imageId);
		if (image is null) {
			return NotFound("Image not found");
		}

		context.WeightStyleImages.Remove(image);
		await context.SaveChangesAsync();
		
		return Ok();
	}

	/// <summary>
	/// Add weight style to product
	/// </summary>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPost("{productId}/styles/{styleId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> AddWeightStyleToProduct(ulong productId, int styleId)
	{
		var product = await context.Products.FindAsync(productId);
		if (product is null) {
			return NotFound("Product not found");
		}
		
		var style = await context.WeightStyles.FindAsync(styleId);
		if (style is null) {
			return NotFound("Style not found");
		}
		
		if (product.ProductWeightStyles.Exists(w => w.WeightStyleId == styleId)) {
			return BadRequest("Style already attached to this product");
		}

		var newLink = new ProductWeightStyle {
			WeightStyleId = styleId,
			ProductId = productId
		};
		
		context.ProductWeightStyles.Add(newLink);
		await context.SaveChangesAsync();
		
		return Ok();
	}
	
	/// <summary>
	/// Remove weight style from product
	/// </summary>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpDelete("{productId}/styles/{styleId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> RemoveWeightStyleFromProduct(ulong productId, int styleId)
	{
		var link = await context.ProductWeightStyles
			.Where(p => p.ProductId == productId && p.WeightStyleId == styleId)
			.FirstOrDefaultAsync();
		
		if (link is null) {
			return NotFound("Link not found");
		}
		
		context.ProductWeightStyles.Remove(link);
		await context.SaveChangesAsync();
		
		return Ok();
	}
}