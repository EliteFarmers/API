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
		var rsp = await App.AnonymousClient.POSTAsync<BanPlayerEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,
				Body = new() {
					PlayerUuid = "some-uuid"
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task BanParticipation_WithoutAuth_ReturnsUnauthorized() {
		var rsp = await App.AnonymousClient.POSTAsync<BanParticipationEndpoint, BanParticipationRequest>(
			new BanParticipationRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,
				Body = new() {
					Uuid = JacobTestApp.TestPlayerUuid,
					Crop = "Wheat",
					Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task AddExcludedTimespan_WithoutAuth_ReturnsUnauthorized() {
		var rsp = await App.AnonymousClient.POSTAsync<AddExcludedTimespanEndpoint, AddExcludedTimespanRequest>(
			new AddExcludedTimespanRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,
				Body = new() {
					Start = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(),
					End = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
					Reason = "Test exclusion"
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task BanPlayer_AsAdmin_BansPlayerAndRemovesFromLeaderboards() {
		// First submit a score so there's something on the leaderboard
		await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,
				LeaderboardId = JacobTestApp.TestLeaderboardId,
				DiscordUserId = (long)JacobTestApp.TestUser2Id,
				UserRoleIds = []
			});

		// Verify the entry exists before banning
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId,
				cancellationToken: TestContext.Current.CancellationToken);
			guild.ShouldNotBeNull();
			var lb = guild.Features.JacobLeaderboard!.Leaderboards.First(l => l.Id == JacobTestApp.TestLeaderboardId);
			lb.Crops.Wheat.ShouldContain(e => e.Uuid == JacobTestApp.TestPlayer2Uuid);
		}

		// Ban the player via admin endpoint
		var rsp = await App.GuildAdminClient.POSTAsync<BanPlayerEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,
				Body = new() {
					PlayerUuid = JacobTestApp.TestPlayer2Uuid
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		// Verify the player is banned and removed from leaderboards
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId,
				cancellationToken: TestContext.Current.CancellationToken);
			guild.ShouldNotBeNull();

			guild.Features.JacobLeaderboard!.BlockedPlayerUuids.ShouldContain(JacobTestApp.TestPlayer2Uuid);

			var lb = guild.Features.JacobLeaderboard!.Leaderboards.First(l => l.Id == JacobTestApp.TestLeaderboardId);
			lb.Crops.Wheat.ShouldNotContain(e => e.Uuid == JacobTestApp.TestPlayer2Uuid);
		}
	}

	[Fact]
	public async Task BanPlayer_AlreadyBanned_ReturnsConflict() {
		// First ban the player
		await App.GuildAdminClient.POSTAsync<BanPlayerEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,
				Body = new() {
					PlayerUuid = JacobTestApp.TestPlayer3Uuid
				}
			});

		// Try to ban again
		var rsp = await App.GuildAdminClient.POSTAsync<BanPlayerEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,

				Body = new() {
					PlayerUuid = JacobTestApp.TestPlayer3Uuid
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.Conflict);
	}

	[Fact]
	public async Task BanPlayer_InvalidGuild_ReturnsNotFound() {
		var rsp = await App.GuildAdminClient.POSTAsync<BanPlayerEndpoint, BanPlayerRequest>(
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
		// First ban a player
		await App.GuildAdminClient.POSTAsync<BanPlayerEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,
				Body = new() {
					PlayerUuid = JacobTestApp.TestPlayer4Uuid
				}
			});

		// Verify banned
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId,
				cancellationToken: TestContext.Current.CancellationToken);
			guild!.Features.JacobLeaderboard!.BlockedPlayerUuids.ShouldContain(JacobTestApp.TestPlayer4Uuid);
		}

		// Unban via endpoint
		var rsp = await App.GuildAdminClient.DELETEAsync<UnbanPlayerEndpoint, BanPlayerRequest>(
			new BanPlayerRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,

				Body = new() {
					PlayerUuid = JacobTestApp.TestPlayer4Uuid
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		// Verify unbanned
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId,
				cancellationToken: TestContext.Current.CancellationToken);
			guild!.Features.JacobLeaderboard!.BlockedPlayerUuids.ShouldNotContain(JacobTestApp.TestPlayer4Uuid);
		}
	}

	[Fact]
	public async Task BanParticipation_AsAdmin_BansParticipationAndRemovesFromLeaderboards() {
		// First submit a score
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600;

		// Create a score with a known timestamp
		await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
			new SubmitScoreRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,
				LeaderboardId = JacobTestApp.TestLeaderboardId,
				DiscordUserId = (long)JacobTestApp.TestUserId,
				UserRoleIds = []
			});

		// Get the exact timestamp from the record to ban it.
		long recordTimestamp = 0;
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId,
				cancellationToken: TestContext.Current.CancellationToken);
			var lb = guild!.Features.JacobLeaderboard!.Leaderboards.First(l => l.Id == JacobTestApp.TestLeaderboardId);
			var entry = lb.Crops.Wheat.First(e => e.Uuid == JacobTestApp.TestPlayerUuid);
			recordTimestamp = entry.Record.Timestamp;
		}

		// Ban the participation
		var rsp = await App.GuildAdminClient.POSTAsync<BanParticipationEndpoint, BanParticipationRequest>(
			new BanParticipationRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,
				Body = new() {
					Uuid = JacobTestApp.TestPlayerUuid,
					Crop = "Wheat",
					Timestamp = recordTimestamp
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		// Verify banned and removed
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId,
				cancellationToken: TestContext.Current.CancellationToken);

			// Key format: timestamp-crop-uuid
			var key = $"{recordTimestamp}-Wheat-{JacobTestApp.TestPlayerUuid}";
			guild!.Features.JacobLeaderboard!.ExcludedParticipations.ShouldContain(key);

			var lb = guild.Features.JacobLeaderboard!.Leaderboards.First(l => l.Id == JacobTestApp.TestLeaderboardId);
			lb.Crops.Wheat.ShouldNotContain(e => e.Uuid == JacobTestApp.TestPlayerUuid);
		}
	}

	[Fact]
	public async Task UnbanParticipation_AsAdmin_UnbansParticipation() {
		// Ban a participation first
		var timestamp = 1234567890;
		var crop = "Wheat";
		var uuid = JacobTestApp.TestPlayer2Uuid;
		var key = $"{timestamp}-{crop}-{uuid}";

		// Manually add ban to DB to set up state
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId,
				cancellationToken: TestContext.Current.CancellationToken);
			guild!.Features.JacobLeaderboard!.ExcludedParticipations.Add(key);
			db.Guilds.Update(guild);
			await db.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		// Unban
		var rsp = await App.GuildAdminClient.DELETEAsync<UnbanParticipationEndpoint, UnbanParticipationRequest>(
			new UnbanParticipationRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,
				ParticipationId = key
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		// Verify unbanned
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<DataContext>();
			var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId,
				cancellationToken: TestContext.Current.CancellationToken);
			guild!.Features.JacobLeaderboard!.ExcludedParticipations.ShouldNotContain(key);
		}
	}

	#endregion

	[Fact]
	public async Task AddExcludedTimespan_AsAdmin_AddsTimespan() {
		var start = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
		var end = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var reason = "Test maintenance window";

		var rsp = await App.GuildAdminClient.POSTAsync<AddExcludedTimespanEndpoint, AddExcludedTimespanRequest>(
			new AddExcludedTimespanRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,
				Body = new() {
					Start = start,
					End = end,
					Reason = reason
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		using var scope = App.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<DataContext>();
		var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId,
			cancellationToken: TestContext.Current.CancellationToken);
		guild.ShouldNotBeNull();
		guild.Features.JacobLeaderboard!.ExcludedTimespans.ShouldContain(t =>
			t.Start == start && t.End == end && t.Reason == reason);
	}

	[Fact]
	public async Task RemoveExcludedTimespan_AsAdmin_RemovesTimespan() {
		var start = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds();
		var end = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
		var reason = "To be removed";

		// Add a timespan first
		await App.GuildAdminClient.POSTAsync<AddExcludedTimespanEndpoint, AddExcludedTimespanRequest>(
			new AddExcludedTimespanRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,

				Body = new() {
					Start = start,
					End = end,
					Reason = reason
				}
			});

		// Remove it
		var rsp = await App.GuildAdminClient.DELETEAsync<RemoveExcludedTimespanEndpoint, RemoveExcludedTimespanRequest>(
			new RemoveExcludedTimespanRequest {
				DiscordId = (long)JacobTestApp.TestGuildId,

				Body = new() {
					Start = start,
					End = end
				}
			});

		rsp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

		using var scope = App.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<DataContext>();
		var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId,
			cancellationToken: TestContext.Current.CancellationToken);
		guild.ShouldNotBeNull();
		guild.Features.JacobLeaderboard!.ExcludedTimespans.ShouldNotContain(t =>
			t.Start == start && t.End == end);
	}
}