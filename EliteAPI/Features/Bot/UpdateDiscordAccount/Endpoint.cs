using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Bot.UpdateDiscordAccount;

internal sealed class UpdateDiscordAccountEndpoint(
	DataContext context,
	IAccountService accountService,
	AutoMapper.IMapper mapper
) : Endpoint<IncomingAccountDto, AuthorizedAccountDto> {
	
	public override void Configure() {
		Patch("/bot/account");
		Options(o => o.WithMetadata(new ServiceFilterAttribute(typeof(DiscordBotOnlyFilter))));
		Version(0);

		Summary(s => {
			s.Summary = "Update user Discord account";
		});
	}

	public override async Task HandleAsync(IncomingAccountDto request, CancellationToken c) {
		var exising = await accountService.GetAccount(request.Id);
        
		var account = exising ?? new EliteAccount {
			Id = request.Id,
			Username = request.Username,
			DisplayName = request.DisplayName ?? request.Username,
		};

		account.Avatar = request.Avatar ?? account.Avatar;
		account.DisplayName = request.DisplayName ?? account.DisplayName;
		account.Locale = request.Locale ?? account.Locale;
        
		account.Discriminator = request.Discriminator;

		if (exising is null) {
			try {
				await context.Accounts.AddAsync(account, c);
				await context.SaveChangesAsync(c);
			} catch (Exception) {
				ThrowError("Failed to create account");
			}
		} else {
			context.Accounts.Update(account);
		}
        
		await SendAsync(mapper.Map<AuthorizedAccountDto>(account), cancellation: c);
	}
}