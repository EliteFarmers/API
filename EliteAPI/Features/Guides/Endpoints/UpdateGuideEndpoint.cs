using EliteAPI.Features.Guides.Models;
using EliteAPI.Features.Guides.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Guides.Endpoints;

public class UpdateGuideEndpoint(GuideService guideService) : Endpoint<UpdateGuideRequest, UpdateGuideResponse>
{
	public override void Configure() {
		Put("/guides/{Id}");
		Summary(s => {
			s.Summary = "Update a guide draft";
			s.Description = "Update the draft version of a guide. Only the author can update their own guide.";
		});
	}

	public override async Task HandleAsync(UpdateGuideRequest req, CancellationToken ct) {
		var userId = User.GetDiscordId();
		if (userId is null) {
			await Send.UnauthorizedAsync(ct);
			return;
		}

		var guide = await guideService.GetByIdAsync(req.Id);
		if (guide == null) {
			await Send.NotFoundAsync(ct);
			return;
		}

		var isAuthor = guide.AuthorId == userId.Value;
		var isModerator = User.IsModeratorOrHigher();

		if (!isAuthor && !isModerator) {
			await Send.ForbiddenAsync(ct);
			return;
		}

		try {
			var newVersion = await guideService.UpdateDraftAsync(req.Id, req.Title, req.Description,
				req.MarkdownContent, req.IconSkyblockId, req.Tags, req.RichBlocks, req.ConcurrencyVersion);

			await Send.OkAsync(new UpdateGuideResponse { ConcurrencyVersion = newVersion }, ct);
		}
		catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException) {
			ThrowError("The guide has been modified elsewhere. Please refresh to see the latest changes.", 409);
		}
	}
}

public class UpdateGuideRequest
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? IconSkyblockId { get; set; }
	public string Description { get; set; } = string.Empty;
	public string MarkdownContent { get; set; } = string.Empty;
	public List<string>? Tags { get; set; }
	public GuideRichData? RichBlocks { get; set; }
	public int ConcurrencyVersion { get; set; }
}

public class UpdateGuideResponse
{
	public int ConcurrencyVersion { get; set; }
}