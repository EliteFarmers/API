using EliteAPI.Services.AccountService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EliteAPI.Authentication;

public class ApiKeyAuthFilter : ServiceFilterAttribute, IAsyncAuthorizationFilter
{
    public ApiKeyAuthFilter(Type type) : base(type) { }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue("API-Key", out var extractedKey))
        {
            context.Result = new UnauthorizedObjectResult("Missing API key");
            return;
        }

        using var scope = context.HttpContext.RequestServices.CreateScope();
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var apiKey = extractedKey.ToString().Split(" ")[1];
        var account = await accountService.GetAccountByApiKey(apiKey);

        if (account is null)
        {
            context.Result = new UnauthorizedObjectResult("Invalid API key");
        }
    }
}