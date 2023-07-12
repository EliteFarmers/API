using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EliteAPI.Authentication;

public class DiscordAuthFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get authorization tokens
        if (context.HttpContext.Request.Headers.Authorization.Count < 1) {
            context.Result = new UnauthorizedObjectResult("Discord authentication header missing");
            return;
        }

        var auth = context.HttpContext.Request.Headers.Authorization.ToString();
            
        if (!auth.StartsWith("Bearer "))
        {
            context.Result = new UnauthorizedObjectResult("Discord authentication header missing");
            return;
        }
        
        using var scopes = context.HttpContext.RequestServices.CreateScope();
        var discordServices = scopes.ServiceProvider.GetRequiredService<IDiscordService>();

        var account = await discordServices.GetDiscordUser(auth.Replace("Bearer ", ""));
        
        if (account is null)
        {
            context.Result = new UnauthorizedObjectResult("Failed to authenticate user");
            return;
        }
        
        context.HttpContext.Items.Add("Account", account);
    }
}