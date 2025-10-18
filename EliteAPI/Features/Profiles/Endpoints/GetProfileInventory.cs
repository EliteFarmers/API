using System.Text.Json.Serialization;
using EliteAPI.Data;
using EliteAPI.Features.Profiles.Mappers;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Models.Common;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Profiles.Endpoints;

public class GetProfileInventoryRequest : PlayerUuidRequest
{
	public required string ProfileUuid { get; set; }

	[JsonIgnore] public string ProfileUuidFormatted => ProfileUuid.ToLowerInvariant().Replace("-", "");

	/// <summary>
	/// Inventory ID
	/// </summary>
	public required Guid Inventory { get; set; }
}

internal sealed class GetProfileInventoryEndpoint(
	IMemberService memberService,
	DataContext context
) : Endpoint<GetProfileInventoryRequest, HypixelInventoryDto>
{
	public override void Configure() {
		Get("/profile/{PlayerUuid}/{ProfileUuid}/inventories/{Inventory}");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Specific Profile Member Inventory"; });
	}

	public override async Task HandleAsync(GetProfileInventoryRequest request, CancellationToken c) {
		var memberId = await memberService.GetProfileMemberId(request.PlayerUuidFormatted, request.ProfileUuidFormatted);
		if (memberId is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var inventory = await context.HypixelInventory
			.Include(i => i.Items)
			.FirstOrDefaultAsync(x => x.ProfileMemberId == memberId && x.HypixelInventoryId == request.Inventory, c);

		if (inventory is null) {
			await Send.NotFoundAsync(c);
			return;
		}
		
		await Send.OkAsync(inventory.ToDto(), c);
	}
}

internal sealed class GetProfileInventoryRequestValidator : Validator<GetProfileInventoryRequest>
{
	public GetProfileInventoryRequestValidator() {
		Include(new PlayerUuidRequestValidator());
		RuleFor(x => x.ProfileUuid)
			.NotEmpty()
			.WithMessage("PlayerUuid is required")
			.Matches("^[a-fA-F0-9-]{32,36}$")
			.WithMessage("PlayerUuid must match ^[a-fA-F0-9-]{32,36}$");

		RuleFor(x => x.Inventory)
			.NotEmpty()
			.WithMessage("Inventory uuid is required");
	}
}