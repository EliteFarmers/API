using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Confirmations.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Confirmations.Services;

public interface IConfirmationService
{
	Task<Confirmation?> GetPendingConfirmationAsync(ApiUser user);
}

[RegisterService<IConfirmationService>(LifeTime.Scoped)]
public class ConfirmationService(DataContext context) : IConfirmationService
{
	public async Task<Confirmation?> GetPendingConfirmationAsync(ApiUser user)
	{
		var pendingConfirmation = await context.Confirmations
			.Where(c => c.IsActive && !c.UserConfirmations.Any(uc => uc.UserId == user.Id))
			.OrderBy(c => c.CreatedAt)
			.FirstOrDefaultAsync();
        
		return pendingConfirmation;
	}
}