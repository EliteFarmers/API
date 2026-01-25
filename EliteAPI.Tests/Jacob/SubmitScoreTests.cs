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
    [Fact, Priority(1)]
    public async Task SubmitScore_WithoutBotAuth_ReturnsForbidden()
    {
        var (rsp, _) = await App.AnonymousClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
            new SubmitScoreRequest
            {
                DiscordId = (long)JacobTestApp.TestGuildId,
                LeaderboardId = JacobTestApp.TestLeaderboardId,
                DiscordUserId = (long)JacobTestApp.TestUserId,
                UserRoleIds = []
            });

        rsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact, Priority(2)]
    public async Task SubmitScore_WithBotAuth_MissingDiscordUserId_ReturnsBadRequest()
    {
        var (rsp, _) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
            new SubmitScoreRequest
            {
                DiscordId = (long)JacobTestApp.TestGuildId,
                LeaderboardId = JacobTestApp.TestLeaderboardId,
                DiscordUserId = null,
                UserRoleIds = []
            });

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact, Priority(3)]
    public async Task SubmitScore_WithBotAuth_InvalidGuild_ReturnsNotFound()
    {
        var (rsp, _) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
            new SubmitScoreRequest
            {
                DiscordId = 999999999999999999,
                LeaderboardId = JacobTestApp.TestLeaderboardId,
                DiscordUserId = (long)JacobTestApp.TestUserId,
                UserRoleIds = []
            });

        rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact, Priority(4)]
    public async Task SubmitScore_WithBotAuth_InvalidLeaderboard_ReturnsNotFound()
    {
        var (rsp, _) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
            new SubmitScoreRequest
            {
                DiscordId = (long)JacobTestApp.TestGuildId,
                LeaderboardId = "invalid-lb-id",
                DiscordUserId = (long)JacobTestApp.TestUserId,
                UserRoleIds = []
            });

        rsp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact, Priority(5)]
    public async Task SubmitScore_ValidRequest_AddsScoreToLeaderboard()
    {
        // Setup: Remove the existing entry for the user so it counts as a new score
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId, cancellationToken: TestContext.Current.CancellationToken);
            var lb = guild!.Features.JacobLeaderboard!.Leaderboards.First(l => l.Id == JacobTestApp.TestLeaderboardId);
            var entry = lb.Crops.Wheat.FirstOrDefault(e => e.Uuid == JacobTestApp.TestPlayerUuid);
            if (entry != null)
            {
               lb.Crops.Wheat.Remove(entry);
               db.Guilds.Update(guild);
               await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
        }

        var (rsp, result) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
            new SubmitScoreRequest
            {
                DiscordId = (long)JacobTestApp.TestGuildId,
                LeaderboardId = JacobTestApp.TestLeaderboardId,
                DiscordUserId = (long)JacobTestApp.TestUserId,
                UserRoleIds = []
            });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        result.Changes.Count.ShouldBeGreaterThan(0);
        
        var change = result.Changes.First();
        change.Crop.ShouldBe("Wheat");
        change.Submitter.Uuid.ShouldBe(JacobTestApp.TestPlayerUuid);
        change.Submitter.Ign.ShouldBe(JacobTestApp.TestPlayerIgn);
        change.NewPosition.ShouldBe(0);
        change.Record.ShouldNotBeNull();
        change.Record.Collected.ShouldBe(600000);
    }

    [Fact, Priority(6)]
    public async Task SubmitScore_ResponseShape_ContainsCorrectFields()
    {
        var (rsp, result) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
            new SubmitScoreRequest
            {
                DiscordId = (long)JacobTestApp.TestGuildId,
                LeaderboardId = JacobTestApp.TestLeaderboardId,
                DiscordUserId = (long)JacobTestApp.TestUser2Id,
                UserRoleIds = []
            });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        
        if (result.Changes.Count > 0)
        {
            var change = result.Changes.First();
            change.Crop.ShouldNotBeNullOrEmpty();
            change.Submitter.ShouldNotBeNull();
            change.Record.ShouldNotBeNull();
            change.Record.Crop.ShouldNotBeNullOrEmpty();
            change.Record.Timestamp.ShouldBeGreaterThan(0);
            change.Record.Collected.ShouldBeGreaterThan(0);
        }
    }

    [Fact, Priority(7)]
    public async Task SubmitScore_LowerScore_NotAddedToFullLeaderboard()
    {
        // First three players submit their scores
        await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
            new SubmitScoreRequest
            {
                DiscordId = (long)JacobTestApp.TestGuildId,
                LeaderboardId = JacobTestApp.TestLeaderboardId,
                DiscordUserId = (long)JacobTestApp.TestUserId,
                UserRoleIds = []
            });
        
        await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
            new SubmitScoreRequest
            {
                DiscordId = (long)JacobTestApp.TestGuildId,
                LeaderboardId = JacobTestApp.TestLeaderboardId,
                DiscordUserId = (long)JacobTestApp.TestUser2Id,
                UserRoleIds = []
            });
        
        await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
            new SubmitScoreRequest
            {
                DiscordId = (long)JacobTestApp.TestGuildId,
                LeaderboardId = JacobTestApp.TestLeaderboardId,
                DiscordUserId = (long)JacobTestApp.TestUser3Id,
                UserRoleIds = []
            });
        
        // Fourth player (300000) tries to submit - lower than 3rd place (400000), should not be added
        var (rsp, result) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
            new SubmitScoreRequest
            {
                DiscordId = (long)JacobTestApp.TestGuildId,
                LeaderboardId = JacobTestApp.TestLeaderboardId,
                DiscordUserId = (long)JacobTestApp.TestUser4Id,
                UserRoleIds = []
            });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        // User 4 has 300000 which is less than User 3's 400000, so no changes for wheat
        var wheatChange = result.Changes.FirstOrDefault(c => c.Crop == "Wheat");
        wheatChange.ShouldBeNull();
    }

    [Fact, Priority(8)]
    public async Task SubmitScore_BannedPlayer_ReturnsForbidden()
    {
        // Directly ban player in DB since we can't use admin endpoints without proper auth
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var guild = await db.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId, cancellationToken: TestContext.Current.CancellationToken);
        guild.ShouldNotBeNull();
        guild.Features.JacobLeaderboard!.BlockedPlayerUuids.Add(JacobTestApp.TestPlayer4Uuid);
        db.Guilds.Update(guild);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear(); // Clear tracking to ensure fresh read in endpoint

        // Then try to submit - should be forbidden
        var (rsp, _) = await App.BotClient.POSTAsync<SubmitScoreEndpoint, SubmitScoreRequest, SubmitScoreResponse>(
            new SubmitScoreRequest
            {
                DiscordId = (long)JacobTestApp.TestGuildId,
                LeaderboardId = JacobTestApp.TestLeaderboardId,
                DiscordUserId = (long)JacobTestApp.TestUser4Id,
                UserRoleIds = []
            });

        rsp.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Cleanup
        using var scope2 = App.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<DataContext>();
        var guild2 = await db2.Guilds.FirstOrDefaultAsync(g => g.Id == JacobTestApp.TestGuildId, cancellationToken: TestContext.Current.CancellationToken);
        if (guild2 != null)
        {
            guild2.Features.JacobLeaderboard!.BlockedPlayerUuids.Remove(JacobTestApp.TestPlayer4Uuid);
            db2.Guilds.Update(guild2);
            await db2.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }
}
