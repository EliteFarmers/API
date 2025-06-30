using EliteAPI.Data;
using FastEndpoints;

namespace EliteAPI.Features.Articles;

interface IArticleService
{
    Task CreateArticleAsync(CreateArticleDto dto);
}

[RegisterService<IArticleService>(LifeTime.Scoped)]
public class ArticleService(DataContext context): IArticleService
{
    public Task CreateArticleAsync(CreateArticleDto dto)
    {
        throw new NotImplementedException();
    }
}