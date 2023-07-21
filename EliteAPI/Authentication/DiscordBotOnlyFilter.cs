using EliteAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EliteAPI.Authentication;

public class DiscordBotOnlyFilter : IAsyncAuthorizationFilter {
    
    public Task OnAuthorizationAsync(AuthorizationFilterContext context) {
        if (context.HttpContext.Request.Headers.Authorization.Count < 1) {
            context.Result = new UnauthorizedObjectResult("Only the bot can access this endpoint.");
            return Task.CompletedTask;
        }
        
        var auth = context.HttpContext.Request.Headers.Authorization.ToString();
        
        if (context.HttpContext.Connection.RemoteIpAddress?.IsFromDockerNetwork() != true || !auth.StartsWith("Bearer EliteDiscordBot ")) {
            context.Result = new UnauthorizedObjectResult("Only the bot can access this endpoint.");
            return Task.CompletedTask;
        }

        if (auth.Replace("Bearer EliteDiscordBot ", "") == Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")) {
            // Success
            return Task.CompletedTask;
        }
        
        context.Result = new UnauthorizedObjectResult("Only the bot can access this endpoint.");
        return Task.CompletedTask;
    }
}