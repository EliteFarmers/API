using System.Text.Json;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Services;
using ErrorOr;
using FastEndpoints;

namespace EliteAPI.Utilities;

public static class UseFastEndpoints
{

	public static WebApplicationBuilder AddEliteFastEndpoints(this WebApplicationBuilder builder) {
		builder.Services.AddFastEndpoints(o => { o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All; });
		builder.Services.AddJobQueues<JobRecord, JobStorageProvider>();
		return builder;
	}

	public static WebApplication UseEliteFastEndpoints(this WebApplication app) {
		app.UseFastEndpoints(c => {
			c.Binding.ReflectionCache.AddFromEliteAPI();
			c.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

			c.Binding.UsePropertyNamingPolicy = true;
			c.Versioning.Prefix = "v";
			c.Versioning.PrependToRoute = true;

			c.Endpoints.NameGenerator = (ctx) => {
				var name = (ctx.TagPrefix ?? "") + ctx.EndpointType.Name.Replace("Endpoint", "") +
				           (ctx.RouteNumber?.ToString() ?? "");
				return name;
			};

			c.Endpoints.Configurator = endpoints => {
				if (endpoints.IdempotencyOptions is not null)
					endpoints.IdempotencyOptions.CacheDuration = TimeSpan.FromMinutes(1);

				// Handle ErrorOr responses automatically
				if (endpoints.ResDtoType.IsAssignableTo(typeof(IErrorOr))) {
					endpoints.DontAutoSendResponse();
					endpoints.PostProcessor<ResponseSender>(Order.After);

					// Correct the openapi documentation for ErrorOr responses
					var produces = endpoints.ResDtoType.GetGenericArguments()[0];
					if (produces == typeof(Success) || produces == typeof(Created) || produces == typeof(Updated) ||
					    produces == typeof(Deleted))
						endpoints.Description(b => b.ClearDefaultProduces()
							.Produces(204)
							.ProducesProblemDetails());
					else
						endpoints.Description(b => b.ClearDefaultProduces()
							.Produces(200, endpoints.ResDtoType.GetGenericArguments()[0])
							.ProducesProblemDetails());
				}
			};

			c.Security.RoleClaimType = ClaimNames.Role;
			c.Security.NameClaimType = ClaimNames.Name;
		});

		app.UseJobQueues(o => {
			o.MaxConcurrency = 4;
			o.ExecutionTimeLimit = TimeSpan.FromMinutes(1);
		});

		return app;
	}
}