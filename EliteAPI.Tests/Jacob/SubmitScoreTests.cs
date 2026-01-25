using System.Net;
using EliteAPI.Data;
using EliteAPI.Features.Guilds.User.Jacob.SubmitScore;
using FastEndpoints;
using FastEndpoints.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EliteAPI.Tests.Jacob;

[Collection<JacobTestCollection>]
public class SubmitScoreTests(JacobTestApp App) : TestBase
{
	[Fact]
	public async Task SubmitScore_WithoutBotAuth_ReturnsForbidden() {
		var scenario = await App.CreateScenarioAsync(1);

		var (rsp, _) =
			await App.AnonymousClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
				new SubmitScoreRequest {
					DiscordId = (long)scenario.Guild.Id,
					LeaderboardId = scenario.Guild.LeaderboardId,
					DiscordUserId = (long)scenario.User1.Id,
					UserRoleIds = []
				});

		rsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task SubmitScore_WithBotAuth_MissingDiscordUserId_ReturnsBadRequest() {
		var scenario = await App.CreateScenarioAsync(1);

		var (rsp, _) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)scenario.Guild.Id,
				LeaderboardId = scenario.Guild.LeaderboardId,
				DiscordUserId = null,
				UserRoleIds = []
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task SubmitScore_WithBotAuth_InvalidGuild_ReturnsNotFound() {
		var (rsp, _) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = 999999999999999999,
				LeaderboardId = "invalid-lb-id",
				DiscordUserId = 123456789,
				UserRoleIds = []
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task SubmitScore_WithBotAuth_InvalidLeaderboard_ReturnsNotFound() {
		var scenario = await App.CreateScenarioAsync(1);

		var (rsp, _) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)scenario.Guild.Id,
				LeaderboardId = "invalid-lb-id",
				DiscordUserId = (long)scenario.User1.Id,
				UserRoleIds = []
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task SubmitScore_ValidRequest_AddsScoreToLeaderboard() {
		var scenario = await App.CreateScenarioAsync(1);

		var (rsp, result) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)scenario.Guild.Id,
				LeaderboardId = scenario.Guild.LeaderboardId,
				DiscordUserId = (long)scenario.User1.Id,
				UserRoleIds = []
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
		result.ShouldNotBeNull();
		result.Changes.Count.ShouldBeGreaterThan(0);

		var change = result.Changes.First();
		change.Crop.ShouldBe("Wheat");
		change.Submitter.Uuid.ShouldBe(scenario.User1.PlayerUuid);
		change.Submitter.Ign.ShouldBe(scenario.User1.Ign);
		change.NewPosition.ShouldBe(0);
		change.Record.ShouldNotBeNull();
	}

	[Fact]
	public async Task SubmitScore_ResponseShape_ContainsCorrectFields() {
		var scenario = await App.CreateScenarioAsync(2);

		var (rsp, result) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)scenario.Guild.Id,
				LeaderboardId = scenario.Guild.LeaderboardId,
				DiscordUserId = (long)scenario.User2.Id,
				UserRoleIds = []
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
		result.ShouldNotBeNull();

		if (result.Changes.Count > 0) {
			var change = result.Changes.First();
			change.Crop.ShouldNotBeNullOrEmpty();
			change.Submitter.ShouldNotBeNull();
			change.Record.ShouldNotBeNull();
			change.Record.Crop.ShouldNotBeNullOrEmpty();
			change.Record.Timestamp.ShouldBeGreaterThan(0);
			change.Record.Collected.ShouldBeGreaterThan(0);
		}
	}

	[Fact]
	public async Task SubmitScore_LowerScore_NotAddedToFullLeaderboard() {
		var scenario = await App.CreateScenarioAsync(4);

		// Submit first three players' scores to fill the leaderboard
		await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)scenario.Guild.Id,
				LeaderboardId = scenario.Guild.LeaderboardId,
				DiscordUserId = (long)scenario.User1.Id,
				UserRoleIds = []
			});

		await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)scenario.Guild.Id,
				LeaderboardId = scenario.Guild.LeaderboardId,
				DiscordUserId = (long)scenario.User2.Id,
				UserRoleIds = []
			});

		await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)scenario.Guild.Id,
				LeaderboardId = scenario.Guild.LeaderboardId,
				DiscordUserId = (long)scenario.User3.Id,
				UserRoleIds = []
			});

		// Fourth player (300000) tries to submit - lower than 3rd place (400000), should not be added
		var (rsp, result) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)scenario.Guild.Id,
				LeaderboardId = scenario.Guild.LeaderboardId,
				DiscordUserId = (long)scenario.User4.Id,
				UserRoleIds = []
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
		result.ShouldNotBeNull();
		var wheatChange = result.Changes.FirstOrDefault(c => c.Crop == "Wheat");
		wheatChange.ShouldBeNull();
	}

	[Fact]
	public async Task SubmitScore_BannedPlayer_ReturnsForbidden() {
		var scenario = await App.CreateScenarioAsync(1);

		// Ban the player in DB
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
				cancellationToken: TestContext.Current.CancellationToken);
			guild.ShouldNotBeNull();
			guild.Features.JacobLeaderboard!.BlockedPlayerUuids.Add(scenario.User1.PlayerUuid);
			db.Guilds.Update(guild);
			await db.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		var (rsp, _) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)scenario.Guild.Id,
				LeaderboardId = scenario.Guild.LeaderboardId,
				DiscordUserId = (long)scenario.User1.Id,
				UserRoleIds = []
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
	}
}