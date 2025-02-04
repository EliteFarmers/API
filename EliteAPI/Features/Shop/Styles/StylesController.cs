using System.Text.Json;
using System.Web;
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

namespace EliteAPI.Features.Shop.Styles;

[ApiController, ApiVersion(1.0)]
[Route("/product/style")]
[Route("/v{version:apiVersion}/product/style")]
public class StylesController(
	DataContext context,
	IConnectionMultiplexer redis,
	IObjectStorageService objectStorageService,
	IMapper mapper)
	: ControllerBase 
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};
	
	/// <summary>
	/// Get weight styles
	/// </summary>
	/// <returns></returns>
	[HttpGet, Route("/product/styles")]
	[Route("/v{version:apiVersion}/product/styles")]
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
	[HttpGet("{styleId:int}")]
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
	[HttpPost]
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
	[HttpPost("{styleId:int}")]
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
	[HttpDelete("{styleId:int}")]
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
	/// Add image to a cosmetic
	/// </summary>
	/// <param name="styleId"></param>
	/// <param name="imageDto"></param>
	/// <param name="thumbnail">Specify if this image should be the thumbnail</param>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPost("{styleId:int}/images")]
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
	[HttpDelete("{styleId:int}/images/{imagePath}")]
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