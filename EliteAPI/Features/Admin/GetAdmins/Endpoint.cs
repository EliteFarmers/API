using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Admin.GetAdmins;

internal sealed class GetAdminsEndpoint(
	DataContext context)
	: EndpointWithoutRequest<List<AccountWithPermsDto>> 
{
	public override void Configure() {
		Get("/admins");
		Policies(ApiUserPolicies.Moderator);
		Version(0);
		
		Summary(s => {
			s.Summary = "Get list of admins";
		});
	}

	public override async Task HandleAsync(CancellationToken c) 
	{
		// I'm sure this query can be optimized further.
		// Right now it's not expected to handle a large amount of users.
		var users = from user in context.Users
			join account in context.Accounts on user.AccountId equals account.Id
			join userRole in context.UserRoles on user.Id equals userRole.UserId into userRoles
			from userRole in userRoles.DefaultIfEmpty()
			join role in context.Roles on userRole.RoleId equals role.Id into roles
			from role in roles.DefaultIfEmpty()
			where role == null || role.Name != ApiUserPolicies.User
			group new { user, account, role } by new { user.Id, user.UserName }
			into g
			select new AccountWithPermsDto {
				Id = g.Key.Id,
				DisplayName = g.Max(x => x.account.DisplayName),
				Username = g.Key.UserName ?? g.Max(x => x.account.Username),
				Avatar = g.Max(x => x.account.Avatar),
				Discriminator = g.Max(x => x.account.Discriminator),
				Roles = g.Where(x => x.role != null).Select(x => x.role.Name).ToList()
			};
        
		var result = await users
			.AsNoTracking()
			.AsSplitQuery()
			.ToListAsync(cancellationToken: c);
		
		await SendAsync(result, cancellation: c);
	}
}