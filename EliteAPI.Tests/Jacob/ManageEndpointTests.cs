using System.Net;
using EliteAPI.Data;
using EliteAPI.Features.Guilds.User.Jacob.Manage;
using EliteAPI.Features.Guilds.User.Jacob.SubmitScore;
using FastEndpoints;
using FastEndpoints.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EliteAPI.Tests.Jacob;

[Collection<JacobTestCollection>]
public class ManageEndpointTests(JacobTestApp App) : TestBase
{
	#region Authentication Tests

	[Fact]
	public async Task BanPlayer_WithoutAuth_ReturnsUnauthorized() {
		var scenario = await App.CreateScenarioAsync(1);

		var rsp = await App.AnonymousClient.POSTAsync<BanPlayerFromJacobLeaderboardEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = (long)scenario.Guild.Id,
				Body = new() {
					PlayerUuid = scenario.User1.PlayerUuid
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task BanParticipation_WithoutAuth_ReturnsUnauthorized() {
		var scenario = await App.CreateScenarioAsync(1);
		var timestamp = JacobTestApp.ReferenceTime.AddDays(-1).ToUnixTimeSeconds();

		var rsp = await App.AnonymousClient
			.POSTAsync<BanParticipationFromJacobLeaderboardEndpoint, BanParticipationRequest>(
				new BanParticipationRequest {
					DiscordId = (long)scenario.Guild.Id,
					Body = new() {
						Uuid = scenario.User1.PlayerUuid,
						Crop = "Wheat",
						Timestamp = timestamp
					}
				});

		rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task AddExcludedTimespan_WithoutAuth_ReturnsUnauthorized() {
		var scenario = await App.CreateScenarioAsync(1);
		var start = JacobTestApp.ReferenceTime.AddDays(-1).ToUnixTimeSeconds();
		var end = JacobTestApp.ReferenceTime.ToUnixTimeSeconds();

		var rsp = await App.AnonymousClient
			.POSTAsync<AddJacobLeaderboardExcludedTimespanEndpoint, AddExcludedTimespanRequest>(
				new AddExcludedTimespanRequest {
					DiscordId = (long)scenario.Guild.Id,
					Body = new() {
						Start = start,
						End = end,
						Reason = "Test exclusion"
					}
				});

		rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task BanPlayer_AsAdmin_BansPlayerAndRemovesFromLeaderboards() {
		var scenario = await App.CreateScenarioAsync(2);

		// First submit a score so there's something on the leaderboard
		await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)scenario.Guild.Id,
				LeaderboardId = scenario.Guild.LeaderboardId,
				DiscordUserId = (long)scenario.User2.Id,
				UserRoleIds = []
			});

		// Verify the entry exists before banning
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
				cancellationToken: TestContext.Current.CancellationToken);
			guild.ShouldNotBeNull();
			var lb = guild.Features.JacobLeaderboard!.Leaderboards.First(l => l.Id == scenario.Guild.LeaderboardId);
			lb.Crops.Wheat.ShouldContain(e => e.Uuid == scenario.User2.PlayerUuid);
		}

		// Ban the player via admin endpoint
		var rsp = await App.GuildAdminClient.POSTAsync<BanPlayerFromJacobLeaderboardEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = (long)scenario.Guild.Id,
				Body = new() {
					PlayerUuid = scenario.User2.PlayerUuid
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		// Verify the player is banned and removed from leaderboards
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
				cancellationToken: TestContext.Current.CancellationToken);
			guild.ShouldNotBeNull();

			guild.Features.JacobLeaderboard!.BlockedPlayerUuids.ShouldContain(scenario.User2.PlayerUuid);

			var lb = guild.Features.JacobLeaderboard!.Leaderboards.First(l => l.Id == scenario.Guild.LeaderboardId);
			lb.Crops.Wheat.ShouldNotContain(e => e.Uuid == scenario.User2.PlayerUuid);
		}
	}

	[Fact]
	public async Task BanPlayer_AlreadyBanned_ReturnsConflict() {
		var scenario = await App.CreateScenarioAsync(1);

		// First ban the player
		await App.GuildAdminClient.POSTAsync<BanPlayerFromJacobLeaderboardEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = (long)scenario.Guild.Id,
				Body = new() {
					PlayerUuid = scenario.User1.PlayerUuid
				}
			});

		// Try to ban again
		var rsp = await App.GuildAdminClient.POSTAsync<BanPlayerFromJacobLeaderboardEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = (long)scenario.Guild.Id,
				Body = new() {
					PlayerUuid = scenario.User1.PlayerUuid
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.Conflict);
	}

	[Fact]
	public async Task BanPlayer_InvalidGuild_ReturnsNotFound() {
		var rsp = await App.GuildAdminClient.POSTAsync<BanPlayerFromJacobLeaderboardEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = 999999999999999999,
				Body = new() {
					PlayerUuid = "some-uuid"
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UnbanPlayer_AsAdmin_UnbansPlayer() {
		var scenario = await App.CreateScenarioAsync(1);

		// First ban a player
		await App.GuildAdminClient.POSTAsync<BanPlayerFromJacobLeaderboardEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = (long)scenario.Guild.Id,
				Body = new() {
					PlayerUuid = scenario.User1.PlayerUuid
				}
			});

		// Verify banned
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
				cancellationToken: TestContext.Current.CancellationToken);
			guild!.Features.JacobLeaderboard!.BlockedPlayerUuids.ShouldContain(scenario.User1.PlayerUuid);
		}

		// Unban via endpoint
		var rsp = await App.GuildAdminClient.DELETEAsync<UnbanPlayerFromJacobLeaderboardEndpoint, UnbanPlayerRequest>(
			new UnbanPlayerRequest {
				DiscordId = (long)scenario.Guild.Id,
				PlayerUuid = scenario.User1.PlayerUuid
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		// Verify unbanned
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
				cancellationToken: TestContext.Current.CancellationToken);
			guild!.Features.JacobLeaderboard!.BlockedPlayerUuids.ShouldNotContain(scenario.User1.PlayerUuid);
		}
	}

	[Fact]
	public async Task BanParticipation_AsAdmin_BansParticipationAndRemovesFromLeaderboards() {
		var scenario = await App.CreateScenarioAsync(1);

		// First submit a score
		await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)scenario.Guild.Id,
				LeaderboardId = scenario.Guild.LeaderboardId,
				DiscordUserId = (long)scenario.User1.Id,
				UserRoleIds = []
			});

		// Get the exact timestamp from the record to ban it.
		long recordTimestamp = 0;
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
				cancellationToken: TestContext.Current.CancellationToken);
			var lb = guild!.Features.JacobLeaderboard!.Leaderboards.First(l => l.Id == scenario.Guild.LeaderboardId);
			var entry = lb.Crops.Wheat.First(e => e.Uuid == scenario.User1.PlayerUuid);
			recordTimestamp = entry.Record.Timestamp;
		}

		// Ban the participation
		var rsp = await App.GuildAdminClient
			.POSTAsync<BanParticipationFromJacobLeaderboardEndpoint, BanParticipationRequest>(
				new BanParticipationRequest {
					DiscordId = (long)scenario.Guild.Id,
					Body = new() {
						Uuid = scenario.User1.PlayerUuid,
						Crop = "Wheat",
						Timestamp = recordTimestamp
					}
				});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		// Verify banned and removed
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
				cancellationToken: TestContext.Current.CancellationToken);

			var key = $"{recordTimestamp}-Wheat-{scenario.User1.PlayerUuid}";
			guild!.Features.JacobLeaderboard!.ExcludedParticipations.ShouldContain(key);

			var lb = guild.Features.JacobLeaderboard!.Leaderboards.First(l => l.Id == scenario.Guild.LeaderboardId);
			lb.Crops.Wheat.ShouldNotContain(e => e.Uuid == scenario.User1.PlayerUuid);
		}
	}

	[Fact]
	public async Task UnbanParticipation_AsAdmin_UnbansParticipation() {
		var scenario = await App.CreateScenarioAsync(1);

		// Use deterministic timestamp
		var timestamp = JacobTestApp.ReferenceTime.AddHours(-2).ToUnixTimeSeconds();
		var crop = "Wheat";
		var uuid = scenario.User1.PlayerUuid;
		var key = $"{timestamp}-{crop}-{uuid}";

		// Manually add ban to DB to set up state
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
				cancellationToken: TestContext.Current.CancellationToken);
			guild!.Features.JacobLeaderboard!.ExcludedParticipations.Add(key);
			db.Guilds.Update(guild);
			await db.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		// Unban
		var rsp = await App.GuildAdminClient
			.DELETEAsync<UnbanParticipationFromJacobLeaderboardEndpoint, UnbanParticipationRequest>(
				new UnbanParticipationRequest {
					DiscordId = (long)scenario.Guild.Id,
					ParticipationId = key
				});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		// Verify unbanned
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
				cancellationToken: TestContext.Current.CancellationToken);
			guild!.Features.JacobLeaderboard!.ExcludedParticipations.ShouldNotContain(key);
		}
	}

	#endregion

	[Fact]
	public async Task AddExcludedTimespan_AsAdmin_AddsTimespan() {
		var scenario = await App.CreateScenarioAsync(1);
		var start = JacobTestApp.ReferenceTime.AddDays(-1).ToUnixTimeSeconds();
		var end = JacobTestApp.ReferenceTime.ToUnixTimeSeconds();
		var reason = "Test maintenance window";

		var rsp = await App.GuildAdminClient
			.POSTAsync<AddJacobLeaderboardExcludedTimespanEndpoint, AddExcludedTimespanRequest>(
				new AddExcludedTimespanRequest {
					DiscordId = (long)scenario.Guild.Id,
					Body = new() {
						Start = start,
						End = end,
						Reason = reason
					}
				});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		using var scope = App.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<DataContext>();
		var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
			cancellationToken: TestContext.Current.CancellationToken);
		guild.ShouldNotBeNull();
		guild.Features.JacobLeaderboard!.ExcludedTimespans.ShouldContain(t =>
			t.Start == start && t.End == end && t.Reason == reason);
	}

	[Fact]
	public async Task RemoveExcludedTimespan_AsAdmin_RemovesTimespan() {
		var scenario = await App.CreateScenarioAsync(1);
		var start = JacobTestApp.ReferenceTime.AddHours(-2).ToUnixTimeSeconds();
		var end = JacobTestApp.ReferenceTime.AddHours(-1).ToUnixTimeSeconds();
		var reason = "To be removed";

		// Add a timespan via endpoint
		var addRsp = await App.GuildAdminClient
			.POSTAsync<AddJacobLeaderboardExcludedTimespanEndpoint, AddExcludedTimespanRequest>(
				new AddExcludedTimespanRequest {
					DiscordId = (long)scenario.Guild.Id,
					Body = new() {
						Start = start,
						End = end,
						Reason = reason
					}
				});

		// Verify add succeeded
		addRsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		// Verify it was added in DB
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
				cancellationToken: TestContext.Current.CancellationToken);
			guild!.Features.JacobLeaderboard!.ExcludedTimespans.ShouldContain(t =>
				t.Start == start && t.End == end);
		}

		// Remove it via endpoint
		var rsp = await App.GuildAdminClient
			.DELETEAsync<RemoveJacobLeaderboardExcludedTimespanEndpoint, RemoveExcludedTimespanRequest>(
				new RemoveExcludedTimespanRequest {
					DiscordId = (long)scenario.Guild.Id,
					Start = start,
					End = end
				});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		using var finalScope = App.Services.CreateScope();
		var finalDb = finalScope.ServiceProvider.GetRequiredService<DataContext>();
		var finalGuild = await finalDb.Guilds.FirstOrDefaultAsync(g => g.Id == scenario.Guild.Id,
			cancellationToken: TestContext.Current.CancellationToken);
		finalGuild.ShouldNotBeNull();
		finalGuild.Features.JacobLeaderboard!.ExcludedTimespans.ShouldNotContain(t =>
			t.Start == start && t.End == end);
	}
}