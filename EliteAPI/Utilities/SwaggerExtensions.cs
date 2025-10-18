using FastEndpoints.Swagger;
using NSwag;
using NSwag.Generation.AspNetCore;
using Scalar.AspNetCore;

namespace EliteAPI.Utilities;

public static class SwaggerExtensions
{
	public static IServiceCollection AddEliteSwaggerDocumentation(this IServiceCollection services) {
		services
			.SwaggerDocument(o => {
				o.ShortSchemaNames = true;
				o.DocumentSettings = doc => { doc.EliteApiDoc("v0"); };
			})
			.SwaggerDocument(o => {
				o.ShortSchemaNames = true;
				o.MaxEndpointVersion = 1;
				o.DocumentSettings = doc => { doc.EliteApiDoc("v1"); };
			});

		return services;
	}

	public static void EliteApiDoc(this AspNetCoreOpenApiDocumentGeneratorSettings doc, string version) {
		doc.MarkNonNullablePropsAsRequired();
		doc.DocumentName = version;
		doc.Version = version;

		doc.SchemaSettings.FlattenInheritanceHierarchy = true;
		doc.SchemaSettings.SchemaProcessors.Add(new EnumAttributeSchemaProcessor());
	}

	public static WebApplication UseEliteOpenApi(this WebApplication app) {
		app.UseOpenApi(c => {
			c.Path = "/openapi/{documentName}.json";
			c.PostProcess = (document, _) => { document.Info = CreateInfoForApiVersion(document.Info.Version); };
		});
		app.MapScalarApiReference("/", opt => {
			opt.Title = "Elite API Reference";
			opt.Favicon = "https://elitebot.dev/favicon.ico";
		});
		return app;
	}

	private static OpenApiInfo CreateInfoForApiVersion(string version) {
		const string description =
			"""
			A backend API for https://elitebot.dev/ that provides Hypixel Skyblock data.
			<br><br>
			Use of this API requires following the [Elite API TOS](https://elitebot.dev/apiterms). This API is not affiliated with Hypixel or Mojang.
			""";

		var info = new OpenApiInfo {
			Title = "Elite API",
			Version = version,
			Contact = new OpenApiContact {
				Name = "GitHub",
				Url = "https://github.com/EliteFarmers/API"
			},
			License = new OpenApiLicense {
				Name = "GPL-3.0",
				Url = "https://github.com/EliteFarmers/API/blob/master/LICENSE.txt"
			},
			TermsOfService = "https://elitebot.dev/apiterms",
			Description = description
		};

		return info;
	}
}