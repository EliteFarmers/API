using Asp.Versioning;
using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Monetization;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Controllers;

[ApiController, ApiVersion(1.0)]
[Route("/account/{accountId}/[controller]")]
[Route("/v{version:apiVersion}/account/{accountId:long}/[controller]")]
public partial class EntitlementController(
	IMonetizationService monetizationService, 
	IMapper mapper)
	: ControllerBase
{
	/// <summary>
	/// Get all entitlements for a user or guild
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="target"></param>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpGet, Route("/account/{accountId}/[controller]s")]
	[Route("/v{version:apiVersion}/account/{accountId}/[controller]s")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<List<EntitlementDto>>> GetUserEntitlements(ulong accountId, [FromQuery] EntitlementTarget target = EntitlementTarget.User) {
		if (target == EntitlementTarget.User) {
			var entitlements = await monetizationService.GetUserEntitlementsAsync(accountId);
			return mapper.Map<List<EntitlementDto>>(entitlements);
		}

		if (target == EntitlementTarget.Guild) {
			var entitlements = await monetizationService.GetGuildEntitlementsAsync(accountId);
			return mapper.Map<List<EntitlementDto>>(entitlements);
		}

		return BadRequest("Invalid target type");
	}
	
	/// <summary>
	/// Grant a test entitlement to a user or guild
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="productId"></param>
	/// <param name="target"></param>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPost("{productId}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GrantTestEntitlement(ulong accountId, ulong productId, [FromQuery] EntitlementTarget target = EntitlementTarget.User)
	{
		return await monetizationService.GrantTestEntitlementAsync(accountId, productId, target);
	}
	
	/// <summary>
	/// Remove a test entitlement from a user or guild
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="entitlementId"></param>
	/// <param name="target"></param>
	/// <returns></returns>
	[Authorize(ApiUserPolicies.Admin)]
	[HttpDelete("{entitlementId}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> RemoveTestEntitlement(ulong accountId, ulong entitlementId, [FromQuery] EntitlementTarget target = EntitlementTarget.User)
	{
		return await monetizationService.RemoveTestEntitlementAsync(accountId, entitlementId, target);
	}
}