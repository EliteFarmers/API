using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Account.GetAuthAccount;

internal sealed class GetAuthAccountEndpoint(
	DataContext context,
	UserManager<ApiUser> userManager,
	AutoMapper.IMapper mapper
) : EndpointWithoutRequest<AuthorizedAccountDto> {
	
	public override void Configure() {
		Get("/account");
		Version(0);

		Summary(s => {
			s.Summary = "Get Logged-In Account";
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var user = await userManager.GetUserAsync(User);
		if (user?.AccountId is null) {
			ThrowError("Linked account not found", StatusCodes.Status404NotFound);
		}

		var account = await context.Accounts
			.Include(a => a.MinecraftAccounts)
				.ThenInclude(a => a.Badges)
			.Include(a => a.ProductAccesses.Where(e => !e.Revoked))
				.ThenInclude(a => a.Product)
				.ThenInclude(p => p.WeightStyles)
			.Include(a => a.UserSettings)
				.ThenInclude(a => a.WeightStyle)
			.AsSplitQuery()
			.FirstOrDefaultAsync(a => a.Id.Equals(user.AccountId), cancellationToken: c);

		var result = mapper.Map<AuthorizedAccountDto>(account);
		
		await SendAsync(result, cancellation: c);
	}
}