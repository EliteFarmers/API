using Asp.Versioning;
using AutoMapper;
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing.Shop;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Monetization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Shop;

[ApiController, ApiVersion(1.0)]
[Route("/shop/[controller]")]
[Route("/v{version:apiVersion}/shop/[controller]")]
public class CategoryController(
	DataContext context,
	IMapper mapper)
	: ControllerBase {

	/// <summary>
	/// Get all shop categories
	/// </summary>
	/// <returns></returns>
	[HttpGet]
	[OptionalAuthorize]
	[Route("/shop/categories")]
	[Route("/v{version:apiVersion}/shop/categories")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<List<ShopCategoryDto>>> GetCategories([FromQuery] bool includeProducts = false) {
		var admin = User.IsInRole(ApiUserPolicies.Admin) || User.IsInRole(ApiUserPolicies.Moderator);

		if (includeProducts) {
			var results = await context.Categories
				.Include(c => c.Products.Where(p => admin || p.Available))
				.ThenInclude(product => product.ProductCategories)
				.OrderBy(c => c.Order)
				.Where(c => admin || c.Published)
				.ToListAsync();
			
			foreach (var category in results) {
				category.Products = category.Products
					.OrderBy(p => p.ProductCategories.First(pc => pc.CategoryId == category.Id).Order)
					.ToList();
			}
			
			return mapper.Map<List<ShopCategoryDto>>(results);
		} else {
			return await context.Categories
				.OrderBy(c => c.Order)
				.Where(c => admin || c.Published)
				.Select(c => mapper.Map<ShopCategoryDto>(c))
				.ToListAsync();
		}
	}
	
	/// <summary>
	/// Get a shop category with products
	/// </summary>
	/// <returns></returns>
	[HttpGet("{id}")]
	[OptionalAuthorize]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<ActionResult<ShopCategoryDto>> GetCategoryWithProducts(string id) {
		var admin = User.IsInRole(ApiUserPolicies.Admin) || User.IsInRole(ApiUserPolicies.Moderator);
		
		var query = context.Categories
			.Include(c => c.Products.Where(p => admin || p.Available))
			.Where(c => admin || c.Published);
		
		var category = int.TryParse(id, out var categoryId)
			? await query.FirstOrDefaultAsync(c => c.Id == categoryId)
			: await query.FirstOrDefaultAsync(c => c.Slug == id);
		
		if (category is null) {
			return NotFound("Category not found");
		}
		
		category.Products = category.Products
			.OrderBy(p => p.ProductCategories.First(pc => pc.CategoryId == category.Id).Order)
			.ToList();
		
		return mapper.Map<ShopCategoryDto>(category);
	}
	
	/// <summary>
	/// Create a shop category
	/// </summary>
	/// <returns></returns>
	[HttpPost]
	[Authorize(ApiUserPolicies.Admin)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<ActionResult> CreateCategory(CreateCategoryDto dto) {
		var existing = await context.Categories
			.AsNoTracking()
			.FirstOrDefaultAsync(c => c.Slug == dto.Slug);
		
		if (existing is not null) {
			return BadRequest("Category already exists");
		}

		var category = new Category() {
			Description = dto.Description,
			Slug = dto.Slug,
			Title = dto.Title
		};
		
		context.Categories.Add(category);
		await context.SaveChangesAsync();
		
		return Ok();
	}
	
	/// <summary>
	/// Edit a shop category
	/// </summary>
	/// <returns></returns>
	[HttpPatch("{id:int}")]
	[Authorize(ApiUserPolicies.Admin)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<ActionResult> EditCategory(int id, EditCategoryDto dto) {
		var category = await context.Categories
			.FirstOrDefaultAsync(c => c.Id == id);
		
		if (category is null) {
			return NotFound("Category not found");
		}
		
		category.Title = dto.Title ?? category.Title;
		category.Description = dto.Description ?? category.Description;
		category.Published = dto.Published ?? category.Published;
		
		if (dto.Slug is not null && dto.Slug != category.Slug) {
			var existing = await context.Categories
				.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Slug == dto.Slug);
			
			if (existing is not null) {
				return BadRequest("Category already exists");
			}
			
			category.Slug = dto.Slug;
		}

		await context.SaveChangesAsync();
		
		return Ok();
	}
	
	/// <summary>
	/// Delete a shop category
	/// </summary>
	/// <returns></returns>
	[HttpDelete("{id:int}")]
	[Authorize(ApiUserPolicies.Admin)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<ActionResult> DeleteCategory(int id) {
		var category = await context.Categories
			.FirstOrDefaultAsync(c => c.Id == id);
		
		if (category is null) {
			return NotFound("Category not found");
		}
		
		context.Categories.Remove(category);
		await context.SaveChangesAsync();
		
		return Ok();
	}
	
	/// <summary>
	/// Add a product to a shop category
	/// </summary>
	/// <returns></returns>
	[HttpPost("{id:int}/product/{productId}")]
	[Authorize(ApiUserPolicies.Admin)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<ActionResult> AddProductToCategory(int id, ulong productId) {
		var existing = await context.ProductCategories
			.Where(c => c.CategoryId == id)
			.Select(c => new { c.ProductId, c.Order })
			.AsNoTracking()
			.ToListAsync();
		
		if (existing.Exists(c => c.ProductId == productId)) {
			return Ok();
		}

		if (existing.Count == 0) {
			var category = await context.Categories
				.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Id == id);

			if (category is null) {
				return NotFound("Category not found");
			}
		}

		var product = await context.Products
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.Id == productId);
		
		if (product is null) {
			return NotFound("Product not found");
		}
		
		var productCategory = new ProductCategory {
			CategoryId = id,
			ProductId = productId,
			Order = existing.Count + 1
		};
		
		context.ProductCategories.Add(productCategory);
		await context.SaveChangesAsync();
		
		return Ok();
	}
	
	/// <summary>
	/// Remove a product from a shop category
	/// </summary>
	/// <returns></returns>
	[HttpDelete("{id:int}/product/{productId}")]
	[Authorize(ApiUserPolicies.Admin)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
	public async Task<ActionResult> RemoveProductFromCategory(int id, ulong productId) {
		var productCategory = await context.ProductCategories
			.FirstOrDefaultAsync(c => c.CategoryId == id && c.ProductId == productId);
		
		if (productCategory is null) {
			return NotFound("Product not found in category");
		}
		
		context.ProductCategories.Remove(productCategory);
		await context.SaveChangesAsync();
		
		return Ok();
	}
	
	/// <summary>
	/// Reorder shop categories
	/// </summary>
	/// <returns></returns>
	[HttpPost("reorder")]
	[Authorize(ApiUserPolicies.Admin)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<ActionResult> ReorderCategories(List<ReorderElement<int>> ordering) {
		var categories = await context.Categories
			.Select(c => new { c.Id, c.Order })
			.ToListAsync();
		
		if (categories.Count != ordering.Count) {
			return BadRequest("Invalid category count");
		}
		
		if (ordering.Select(c => c.Id).Distinct().Count() != ordering.Count) {
			return BadRequest("Duplicate category ids");
		}
		
		if (ordering.Select(c => c.Id).Except(categories.Select(c => c.Id)).Any()) {
			return BadRequest("Invalid category ids");
		}
		
		if (ordering.Select(c => c.Order).Distinct().Count() != ordering.Count) {
			return BadRequest("Duplicate order values");
		}
		
		var ordered = ordering.OrderBy(o => o.Order).ToList();
		try {
			for (var i = 0; i < ordered.Count; i++) {
				var order = ordered[i];
				var category = categories.FirstOrDefault(c => c.Id == order.Id);
			
				if (category is null) {
					return BadRequest("Invalid category id");
				}
			
				if (category.Order == i) {
					continue;
				}
			
				await context.Categories
					.Where(c => c.Id == category.Id)
					.ExecuteUpdateAsync(c => c.SetProperty(x => x.Order, i));
			}
		} catch {
			return BadRequest("Failed to reorder categories");
		}
		
		return Ok();
	}
	
	/// <summary>
	/// Reorder products in a shop category
	/// </summary>
	/// <returns></returns>
	[HttpPost("{id:int}/reorder")]
	[Authorize(ApiUserPolicies.Admin)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
	public async Task<ActionResult> ReorderProductsInCategory(int id, List<ReorderElement<string>> stringOrdering) {
		var products = await context.ProductCategories
			.Where(c => c.CategoryId == id)
			.Select(c => new { c.ProductId, c.Order })
			.ToListAsync();
		
		var ordering = stringOrdering.Select(c => new ReorderElement<ulong> {
			Id = ulong.TryParse(c.Id, out var result) ? result : 0,
			Order = c.Order
		}).ToList();
		
		if (ordering.Any(c => c.Id == 0)) {
			return BadRequest("Invalid product id");
		}
		
		if (products.Count != ordering.Count) {
			return BadRequest("Invalid product count");
		}
		
		if (ordering.Select(c => c.Id).Distinct().Count() != ordering.Count) {
			return BadRequest("Duplicate product ids");
		}
		
		if (ordering.Select(c => c.Id).Except(products.Select(c => c.ProductId)).Any()) {
			return BadRequest("Invalid product ids");
		}
		
		if (ordering.Select(c => c.Order).Distinct().Count() != ordering.Count) {
			return BadRequest("Duplicate order values");
		}
		
		var ordered = ordering.OrderBy(o => o.Order).ToList();
		try {
			for (var i = 0; i < ordered.Count; i++) {
				var order = ordered[i];
				var product = products.FirstOrDefault(c => c.ProductId == order.Id);
			
				if (product is null) {
					return BadRequest("Invalid product id");
				}
			
				if (product.Order == i) {
					continue;
				}
			
				await context.ProductCategories
					.Where(c => c.CategoryId == id && c.ProductId == product.ProductId)
					.ExecuteUpdateAsync(c => c.SetProperty(x => x.Order, i));
			}
		} catch {
			return BadRequest("Failed to reorder products");
		}
		
		return Ok();
	}
}