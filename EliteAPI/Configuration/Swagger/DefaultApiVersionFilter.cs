using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EliteAPI.Configuration.Swagger;

public partial class DefaultApiVersionFilter(IOptions<ApiExplorerOptions> options) : IDocumentFilter
{
	private ApiVersion DefaultApiVersion => options.Value.DefaultApiVersion;
	private string ApiVersionFormat => options.Value.SubstitutionFormat;


	public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context) {
		var versionSegment = DefaultApiVersion.ToString(ApiVersionFormat);

		foreach (var apiDescription in context.ApiDescriptions) {
			if (apiDescription.RelativePath == null) continue;

			// If the version is default remove paths like: v1/[controller]/
			if (apiDescription.GetApiVersion() == DefaultApiVersion) {
				if (!apiDescription.RelativePath.Contains(versionSegment)) continue;

				var path = "/" + apiDescription.RelativePath;
				swaggerDoc.Paths.Remove(path);
			}
			// If the version is not default remove paths like [controller]/
			else {
				var match = PathRegex().Match(apiDescription.RelativePath);
				if (match.Success) continue;

				var path = "/" + apiDescription.RelativePath;
				swaggerDoc.Paths.Remove(path);
			}
		}
	}

	[GeneratedRegex(@"^\/v\d+")]
	private static partial Regex PathRegex();
}