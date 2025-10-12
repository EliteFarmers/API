using EliteAPI.Data;
using EliteAPI.Features.Account.DTOs;
using EliteAPI.Models.Common;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Account.GetAccountSettings;

internal sealed class GetAccountSettingsEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<DiscordIdRequest, UserSettingsDto> {
	public override void Configure() {
		Get("/account/{DiscordId}/settings");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Account Settings"; });

		ResponseCache(120);
		Options(o => { o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2))); });
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var settings = await context.Accounts
			.Include(a => a.MinecraftAccounts)
			.Include(a => a.UserSettings)
			.ThenInclude(a => a.WeightStyle)
			.Where(a => a.Id == request.DiscordIdUlong)
			.Select(a => mapper.Map<UserSettingsDto>(a.UserSettings))
			.FirstOrDefaultAsync(c);

		if (settings is null) ThrowError("User settings not found", StatusCodes.Status404NotFound);

		await Send.OkAsync(settings, c);
	}
}