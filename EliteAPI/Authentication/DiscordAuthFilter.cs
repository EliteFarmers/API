using EliteAPI.Services.DiscordService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EliteAPI.Authentication;

public class DiscordAuthFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        context.HttpContext.Request.Cookies.TryGetValue("discord_access_token", out var authToken);
        context.HttpContext.Request.Cookies.TryGetValue("discord_refresh_token", out var refreshToken);

        if (authToken is null && refreshToken is null)
        {
            context.Result = new UnauthorizedObjectResult("Discord authentication cookies missing");
            return;
        }

        using var scope = context.HttpContext.RequestServices.CreateScope();
        var discordService = scope.ServiceProvider.GetRequiredService<IDiscordService>();

        var response = await discordService.GetDiscordUser(authToken, refreshToken);

        if (response is null)
        {
            context.HttpContext.Response.Cookies.Delete("discord_access_token");
            context.HttpContext.Response.Cookies.Delete("discord_refresh_token");

            context.Result = new UnauthorizedObjectResult("Failed to authenticate user");
            return;
        }

        // Set cookies
        if (response.AccessTokenExpires is not null)
        {
            context.HttpContext.Response.Cookies.Append("discord_access_token", response.AccessToken, new CookieOptions
            {
                Expires = response.AccessTokenExpires,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
        }

        if (response.RefreshTokenExpires is not null)
        {
            context.HttpContext.Response.Cookies.Append("discord_refresh_token", response.RefreshToken, new CookieOptions
            {
                Expires = response.RefreshTokenExpires,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
        }

        // Set user
        context.HttpContext.Items.Add("Account", response.Account);
    }
}