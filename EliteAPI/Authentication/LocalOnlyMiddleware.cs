using EliteAPI.Services;

namespace EliteAPI.Authentication; 

public class LocalOnlyMiddleware : IMiddleware {
    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        if (context.Connection.RemoteIpAddress is null || context.Connection.RemoteIpAddress.IsFromDockerNetwork()) {
            await next(context);
        }
        else {
            context.Response.StatusCode = 403;
        }
    }
}