using System.Reflection;
using System.Text;
using Asp.Versioning.ApiExplorer;
using EliteAPI.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EliteAPI.Configuration.Swagger;

public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions> 
{
	public void Configure(SwaggerGenOptions options) {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme 
        {
            In = ParameterLocation.Header,
            Description = "Enter Bearer Token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "bearer"
        });
            
        options.DocumentFilter<DefaultApiVersionFilter>();
        options.OperationFilter<SwaggerAuthFilter>();
        
		foreach (var description in provider.ApiVersionDescriptions)
		{
			options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
		}
        
        options.SupportNonNullableReferenceTypes();
        options.EnableAnnotations();
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
	}
	
	private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var text = new StringBuilder("A backend API for https://elitebot.dev/ that provides Hypixel Skyblock data. " +
                                     "Use of this API requires following the [TOS](https://elitebot.dev/apiterms). This API is not affiliated with Hypixel or Mojang.");
        var info = new OpenApiInfo()
        {
            Title = "Elite API",
            Version = description.ApiVersion.ToString(),
            Contact = new OpenApiContact
            {
                Name = "- GitHub",
                Url = new Uri("https://github.com/EliteFarmers/API")
            },
            License = new OpenApiLicense 
            {
                Name = "GPL-3.0",
                Url = new Uri("https://github.com/EliteFarmers/API/blob/master/LICENSE.txt")
            },
            TermsOfService = new Uri("https://elitebot.dev/apiterms")
        };

        if (description.IsDeprecated)
        {
            text.Append("This API version has been deprecated.");
        }

        if (description.SunsetPolicy is { } policy)
        {
            if (policy.Date is { } when)
            {
                text.Append(" The API will be sunset on ")
                    .Append(when.Date.ToShortDateString())
                    .Append('.');
            }

            if (policy.HasLinks)
            {
                text.AppendLine();

                var rendered = false;

                foreach (var link in policy.Links) {
                    if (link.Type != "text/html") continue;
                    
                    if (!rendered)
                    {
                        text.Append("<h4>Links</h4><ul>");
                        rendered = true;
                    }

                    text.Append("<li><a href=\"");
                    text.Append(link.LinkTarget.OriginalString);
                    text.Append("\">");
                    text.Append(
                        StringSegment.IsNullOrEmpty(link.Title)
                            ? link.LinkTarget.OriginalString
                            : link.Title.ToString());
                    text.Append("</a></li>");
                }

                if (rendered)
                {
                    text.Append("</ul>");
                }
            }
        }

        // text.Append("<h4>Additional Information</h4>");
        info.Description = text.ToString();

        return info;
    }
}