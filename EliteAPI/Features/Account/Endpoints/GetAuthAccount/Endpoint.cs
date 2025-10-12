using EliteAPI.Data;
using EliteAPI.Features.Account.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Account.GetAuthAccount;

internal sealed class GetAuthAccountEndpoint(
	DataContext context,
	UserManager userManager,
	AutoMapper.IMapper mapper
) : EndpointWithoutRequest<AuthorizedAccountDto> {
	public override void Configure() {
		Get("/account");
		Version(0);

		Summary(s => { s.Summary = "Get Logged-In Account"; });
	}

	public override async Task HandleAsync(CancellationToken c) {
		var user = await userManager.GetUserAsync(User);
		if (user?.AccountId is null) ThrowError("Linked account not found", StatusCodes.Status404NotFound);

		var account = await context.Accounts
			.Include(a => a.MinecraftAccounts)
			.ThenInclude(a => a.Badges)
			.Include(a => a.ProductAccesses.Where(pa => !pa.Revoked
			                                            && pa.StartDate <= DateTimeOffset.UtcNow
			                                            && (pa.EndDate == null || pa.EndDate >= DateTimeOffset.UtcNow)))
			.ThenInclude(a => a.Product)
			.ThenInclude(p => p.WeightStyles)
			.Include(a => a.UserSettings)
			.ThenInclude(a => a.WeightStyle)
			.Include(a => a.DismissedAnnouncements)
			.AsSplitQuery()
			.FirstOrDefaultAsync(a => a.Id.Equals(user.AccountId), c);

		var result = mapper.Map<AuthorizedAccountDto>(account);

		await Send.OkAsync(result, c);
	}
}