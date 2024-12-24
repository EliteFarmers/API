using System.Net.Mime;
using System.Text.Json;
using System.Web;
using Asp.Versioning;
using AutoMapper;
using EliteAPI.Background.Discord;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Monetization;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;
using StackExchange.Redis;

namespace EliteAPI.Controllers;

[ApiController, ApiVersion(1.0)]
[Route("/[controller]")]
[Route("/v{version:apiVersion}/[controller]")]
public class ProductController(
	DataContext context,
	IMonetizationService monetizationService, 
	IConnectionMultiplexer redis,
	IObjectStorageService objectStorageService,
	ISchedulerFactory schedulerFactory,
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
			.Include(p => p.WeightStyles)
			.Include(p => p.Images)
			.Where(p => p.Available)
			.Select(x => mapper.Map<ProductDto>(x))
			.ToListAsync();
		
		await db.StringSetAsync(key, JsonSerializer.Serialize(list, JsonOptions), TimeSpan.FromMinutes(5));

		return list;
	}
	
	/// <summary>
	/// Get all products
	/// </summary>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Moderator)]
	[HttpGet]
	[Route("/[controller]s/admin")]
	[Route("/v{version:apiVersion}/[controller]s/admin")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<List<ProductDto>>> GetAllProductsAdmin()
	{
		return await context.Products
			.Include(p => p.WeightStyles)
			.Include(p => p.Images)
			.Select(x => mapper.Map<ProductDto>(x))
			.ToListAsync();
	}
	
	/// <summary>
	/// Refresh all products
	/// </summary>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPost]
	[Route("/[controller]s/refresh")]
	[Route("/v{version:apiVersion}/[controller]s/refresh")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<List<ProductDto>>> RefreshProductsAdmin()
	{
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:products");
		
		var scheduler = await schedulerFactory.GetScheduler();
		await scheduler.TriggerJob(RefreshProductsBackgroundJob.Key);
		
		return Ok();
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
			.Include(p => p.Images)
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
	public async Task<IActionResult> UpdateProduct(ulong productId, [FromBody] EditProductDto dto)
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
	/// Add image to a product
	/// </summary>
	/// <param name="productId"></param>
	/// <param name="imageDto"></param>
	/// <param name="thumbnail">Specify if this image should be the thumbnail</param>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPost("{productId}/images")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<IActionResult> AddProductImage(ulong productId, [FromForm] UploadImageDto imageDto, [FromQuery] bool thumbnail = false)
	{
		var product = await context.Products.FindAsync(productId);
		if (product is null) {
			return NotFound("Product not found");
		}
		
		var image = await objectStorageService.UploadImageAsync($"products/{productId}/{Guid.NewGuid()}.png", imageDto.Image);
		
		image.Title = imageDto.Title;
		image.Description = imageDto.Description;

		if (thumbnail) {
			if (product.Thumbnail is not null) {
				await DeleteProductImage(productId, product.Thumbnail.Path);
			}
			
			product.Thumbnail = image;
			product.ThumbnailId = image.Id;
		} else {
			product.Images.Add(image);
		}
		
		await context.SaveChangesAsync();
		
		// Clear the product list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:productlist");
		
		return Ok();
	}
	
	/// <summary>
	/// Delete image from a product
	/// </summary>
	/// <param name="productId"></param>
	/// <param name="imagePath"></param>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpDelete("{productId}/images/{imagePath}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> DeleteProductImage(ulong productId, string imagePath)
	{
		var product = await context.Products.Include(i => i.Images).FirstOrDefaultAsync(p => p.Id == productId);
		if (product is null) {
			return NotFound("Product not found");
		}

		var decoded = HttpUtility.UrlDecode(imagePath);
		var productImage = product.Images.FirstOrDefault(i => decoded.EndsWith(i.Path))
			?? (product.Thumbnail?.Path == decoded ? product.Thumbnail : null);
		
		if (productImage is null) {
			return NotFound("Image not found");
		}
		
		if (product.Thumbnail == productImage) {
			product.Thumbnail = null;
			product.ThumbnailId = null;
		} else {
			product.Images.Remove(productImage);
		}
		
		await context.SaveChangesAsync();
		
		await objectStorageService.DeleteAsync(productImage.Path);
		
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
	public async Task<ActionResult<List<WeightStyleWithDataDto>>> GetWeightStyles() {
		const string key = "bot:stylelist";
		var db = redis.GetDatabase();
		
		if (db.KeyExists(key)) {
			var styleList = await db.StringGetAsync(key);
			if (styleList.HasValue) {
				var value = JsonSerializer.Deserialize<List<WeightStyleWithDataDto>>(styleList!, JsonOptions);
				if (value is not null) {
					return value;
				}
			}
		}
		
		var existing = await context.WeightStyles
			.Select(s => mapper.Map<WeightStyleWithDataDto>(s))
			.ToListAsync();
		
		await db.StringSetAsync(key, JsonSerializer.Serialize(existing, JsonOptions), TimeSpan.FromMinutes(30));

		return existing;
	}
	
	/// <summary>
	/// Get a weight style
	/// </summary>
	/// <returns></returns>
	[HttpGet("style/{styleId:int}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<ActionResult<WeightStyleWithDataDto>> GetWeightStyle(int styleId) {
		var key = $"bot:stylelist:{styleId}";
		var db = redis.GetDatabase();
		
		if (db.KeyExists(key)) {
			var styleList = await db.StringGetAsync(key);
			if (styleList.HasValue) {
				var value = JsonSerializer.Deserialize<WeightStyleWithDataDto>(styleList!, JsonOptions);
				if (value is not null) {
					return value;
				}
			}
		}
		
		var existing = await context.WeightStyles
			.Include(s => s.Images)
			.Where(s => s.Id == styleId)
			.FirstOrDefaultAsync();
		
		if (existing is null) {
			return NotFound("Weight style not found");
		}
		
		var mapped = mapper.Map<WeightStyleWithDataDto>(existing);

		await db.StringSetAsync(key, JsonSerializer.Serialize(mapped, JsonOptions), TimeSpan.FromMinutes(30));

		return mapped;
	}
	
	/// <summary>
	/// Create a weight style
	/// </summary>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPost("style")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<IActionResult> CreateWeightStyle([FromBody] WeightStyleWithDataDto incoming) {
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
		
		// Clear the style list cache
		await redis.GetDatabase().KeyDeleteAsync("bot:stylelist");
		
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
		
		// Clear the style list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:stylelist");
		await db.KeyDeleteAsync($"bot:stylelist:{styleId}");
		
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
		
		// Clear the style list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:stylelist");
		await db.KeyDeleteAsync($"bot:stylelist:{styleId}");
		
		return Ok();
	}
	
	/// <summary>
	/// Set image for a weight style
	/// </summary>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPut("style/{styleId:int}/image")]
	[RequestSizeLimit(500 * 1024 * 1024)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<IActionResult> SetWeightStyleImage(int styleId, [FromForm] UploadImageDto imageDto)
	{
		var style = await context.WeightStyles.FindAsync(styleId);
		if (style is null) {
			return NotFound("Style not found");
		}
		
		if (style.Image is not null) {
			await DeleteWeightStyleImage(styleId);
		}
		
		var path = $"cosmetics/weightstyles/{styleId}/{Path.GetExtension(imageDto.Image.FileName).ToLowerInvariant()}";
		var newImage = await objectStorageService.UploadImageAsync(path, imageDto.Image);
		
		newImage.Title = imageDto.Title;
		newImage.Description = imageDto.Description;

		context.Images.Add(newImage);
		style.Image = newImage;
		
		await context.SaveChangesAsync();
		
		// Clear the style list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:stylelist");
		await db.KeyDeleteAsync($"bot:stylelist:{styleId}");
		
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
	public async Task<IActionResult> DeleteWeightStyleImage(int styleId)
	{
		var style = await context.WeightStyles.FindAsync(styleId);
		if (style?.Image is null) {
			return NotFound("Image not found");
		}

		await objectStorageService.DeleteAsync(style.Image.Path);

		context.Images.Remove(style.Image);
		style.Image = null;

		await context.SaveChangesAsync();
		
		// Clear the style list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:stylelist");
		await db.KeyDeleteAsync($"bot:stylelist:{styleId}");
		
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
		
		// Clear the style list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:stylelist");
		await db.KeyDeleteAsync($"bot:stylelist:{styleId}");
		
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
		
		// Clear the style list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:stylelist");
		await db.KeyDeleteAsync($"bot:stylelist:{styleId}");
		
		return Ok();
	}
	
	/// <summary>
	/// Add image to a cosmetic
	/// </summary>
	/// <param name="styleId"></param>
	/// <param name="imageDto"></param>
	/// <param name="thumbnail">Specify if this image should be the thumbnail</param>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPost("style/{styleId:int}/images")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<IActionResult> AddStyleImage(int styleId, [FromForm] UploadImageDto imageDto, [FromQuery] bool thumbnail = false)
	{
		var style = await context.WeightStyles.FindAsync(styleId);
		if (style is null) {
			return NotFound("Style not found");
		}
		
		var image = await objectStorageService.UploadImageAsync($"cosmetics/weightstyles/{styleId}/{Guid.NewGuid()}.png", imageDto.Image);
		
		image.Title = imageDto.Title;
		image.Description = imageDto.Description;

		if (thumbnail) {
			if (style.Image is not null) {
				await DeleteStyleImage(styleId, style.Image.Path);
			}
			
			style.Image = image;
		} else {
			style.Images.Add(image);
		}
		
		await context.SaveChangesAsync();
		
		// Clear the product list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:stylelist");
		await db.KeyDeleteAsync($"bot:stylelist:{styleId}");
		
		return Ok();
	}
	
	/// <summary>
	/// Delete image from a style
	/// </summary>
	/// <param name="styleId"></param>
	/// <param name="imagePath"></param>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpDelete("style/{styleId:int}/images/{imagePath}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> DeleteStyleImage(int styleId, string imagePath)
	{
		var style = await context.WeightStyles.Include(i => i.Images).FirstOrDefaultAsync(p => p.Id == styleId);
		if (style is null) {
			return NotFound("Style not found");
		}

		var decoded = HttpUtility.UrlDecode(imagePath);
		var styleImage = style.Images.FirstOrDefault(i => decoded.EndsWith(i.Path))
			?? (style.Image?.Path == decoded ? style.Image : null);
		
		if (styleImage is null) {
			return NotFound("Image not found");
		}
		
		if (style.Image == styleImage) {
			style.Image = null;
		} else {
			style.Images.Remove(styleImage);
		}
		
		await context.SaveChangesAsync();
		
		await objectStorageService.DeleteAsync(styleImage.Path);
		
		// Clear the product list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:stylelist");
		await db.KeyDeleteAsync($"bot:stylelist:{styleId}");
		
		return Ok();
	}
}