namespace EliteAPI.Features.Articles;

public class CreateArticleDto
{
    public required string Title { get; set; } = string.Empty;
    public required string Content { get; set; } = string.Empty;
}