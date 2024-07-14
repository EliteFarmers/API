using Asp.Versioning;
using AutoMapper;
using EliteAPI.Data;
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
	DataContext context,
	IAccountService accountService, 
	IMonetizationService monetizationService, 
	IMapper mapper)
	: ControllerBase
{
	
	[Authorize(ApiUserPolicies.Admin)]
	[HttpPost("{productId}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GrantTestEntitlement(ulong accountId, ulong productId, [FromQuery] EntitlementTarget target = EntitlementTarget.User)
	{
		return await monetizationService.GrantTestEntitlementAsync(accountId, productId, target);
	}
	
	[Authorize(ApiUserPolicies.Admin)]
	[HttpDelete("{productId}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> RemoveTestEntitlement(ulong accountId, ulong productId, [FromQuery] EntitlementTarget target = EntitlementTarget.User)
	{
		return await monetizationService.RemoveTestEntitlementAsync(accountId, productId, target);
	}
}