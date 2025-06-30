using EliteAPI.Features.Auth;
using EliteAPI.Models.Entities.Accounts;
using FastEndpoints;

namespace EliteAPI.Features.Articles;

internal sealed class CreateArticleEndpoint(
    IArticleService articleService
) : Endpoint<CreateArticleDto> 
{
    public override void Configure() {
        Post("/articles/create");
        Policies(ApiUserPolicies.Admin);
        Version(0);

        Summary(s => {
            s.Summary = "Create an article";
            s.Description = "Creates a new article that can be viewed by users";
        });
    }

    public override async Task HandleAsync(CreateArticleDto request, CancellationToken c)
    {
        await articleService.CreateArticleAsync(request);
		
        await SendNoContentAsync(c);
    }
}