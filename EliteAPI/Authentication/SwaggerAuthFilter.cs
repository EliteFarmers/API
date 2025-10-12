using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EliteAPI.Authentication;

/// <summary>
/// Operation filter for SwaggerGen to mark endpoints as requiring authentication
/// </summary>
public class SwaggerAuthFilter : IOperationFilter {
	public void Apply(OpenApiOperation operation, OperationFilterContext context) {
		var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

		// Check if the endpoint has [Authorize] or the DiscordAuthFilter or DiscordBotOnlyFilter
		if (!metadata.Any(x =>
			    x is AuthorizeAttribute or OptionalAuthorizeAttribute || (x is ServiceFilterAttribute filter &&
			                                                              filter.ServiceType ==
			                                                              typeof(DiscordBotOnlyFilter)))) return;

		operation.Security = new List<OpenApiSecurityRequirement> {
			new() {
				{
					new OpenApiSecurityScheme {
						Reference = new OpenApiReference {
							Id = "Bearer",
							Type = ReferenceType.SecurityScheme
						}
					},
					Array.Empty<string>()
				}
			}
		};
	}
}