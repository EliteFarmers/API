using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Hypixel;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Guilds.User.Jacob.SubmitScore;

internal sealed class SubmitScoreEndpoint(
	IDiscordService discordService,
	IAccountService accountService,
	DataContext context
) : Endpoint<SubmitScoreRequest, SubmitScoreResponse>
{
	public override void Configure() {
		Post("/user/guild/{DiscordId}/jacob/{LeaderboardId}/submit");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous();
		Version(0);
		Summary(s => { s.Summary = "Submit scores to a Jacob leaderboard"; });
	}

	public override async Task HandleAsync(SubmitScoreRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		if (!guild.Features.JacobLeaderboardEnabled || guild.Features.JacobLeaderboard is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var feature = guild.Features.JacobLeaderboard;
		var leaderboard = feature.Leaderboards.FirstOrDefault(lb => lb.Id == request.LeaderboardId);

		if (leaderboard?.Crops is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var (submitterDiscordId, submitterRoles) = ResolveSubmitterInfo(request);
		if (submitterDiscordId == 0) {
			ThrowError("DiscordUserId is required.", StatusCodes.Status400BadRequest);
			return;
		}

		var blockedRoles = feature.BlockedRoles.Select(r => r.Id).ToList();
		if (leaderboard.BlockedRole is not null) blockedRoles.Add(leaderboard.BlockedRole);

		if (submitterRoles.Any(r => blockedRoles.Contains(r))) {
			ThrowError("You are not allowed to submit scores.", StatusCodes.Status403Forbidden);
			return;
		}

		var requiredRoles = feature.RequiredRoles.Select(r => r.Id).ToList();
		if (leaderboard.RequiredRole is not null) requiredRoles.Add(leaderboard.RequiredRole);

		if (requiredRoles.Count > 0 && !requiredRoles.All(r => submitterRoles.Contains(r))) {
			ThrowError("You do not have all required roles to submit scores.", StatusCodes.Status403Forbidden);
			return;
		}

		var account = await accountService.GetAccount(submitterDiscordId);
		if (account is null || account.MinecraftAccounts.Count == 0) {
			ThrowError("Account not linked. Please link your Minecraft account first.", StatusCodes.Status400BadRequest);
			return;
		}

		var selectedMc = account.MinecraftAccounts.FirstOrDefault(mc => mc.Selected);
		if (selectedMc is null) {
			ThrowError("No Minecraft account selected.", StatusCodes.Status400BadRequest);
			return;
		}

		if (feature.BlockedPlayerUuids.Contains(selectedMc.Id)) {
			ThrowError("You are banned from this leaderboard.", StatusCodes.Status403Forbidden);
			return;
		}

		var selectedMcId = selectedMc.Id;

		var profileMember = await context.ProfileMembers
			.Include(pm => pm.JacobData)
			.ThenInclude(jd => jd!.Contests)
			.ThenInclude(cp => cp.JacobContest)
			.Where(pm => pm.PlayerUuid == selectedMcId && pm.IsSelected)
			.FirstOrDefaultAsync(c);

		if (profileMember?.JacobData is null || profileMember.JacobData.Contests.Count == 0) {
			ThrowError("No Jacob contests found for this profile.", StatusCodes.Status404NotFound);
			return;
		}

		var lastUpdated = profileMember.LastUpdated;
		var validContests = FilterValidContests(profileMember.JacobData.Contests, leaderboard, feature, selectedMc.Id, lastUpdated);

		if (validContests.Count == 0) {
			await Send.OkAsync(new SubmitScoreResponse(), c);
			return;
		}

		var response = new SubmitScoreResponse();
		var submitterInfo = new SubmitterInfoDto {
			Uuid = selectedMc.Id,
			Ign = selectedMc.Name,
			DiscordId = submitterDiscordId.ToString()
		};

		foreach (var contest in validContests.OrderByDescending(cp => cp.Collected)) {
			var change = ProcessContest(contest, leaderboard, submitterInfo, submitterDiscordId);
			if (change is not null) {
				response.Changes.Add(change);
				if (change.IsNewHighScore ||
				    (change.NewPosition == 0 && (change.Improvement >= 500 || leaderboard.PingForSmallImprovements))) {
					response.ShouldPing = true;
				}
			}
		}

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await Send.OkAsync(response, c);
	}

	private static (ulong DiscordId, List<string> Roles) ResolveSubmitterInfo(SubmitScoreRequest request) {
		// Bot-only endpoint: DiscordUserId is required
		if (request.DiscordUserId is not > 0) {
			return (0, []);
		}

		return ((ulong)request.DiscordUserId.Value, request.UserRoleIds ?? []);
	}

	private static List<ContestParticipation> FilterValidContests(
		List<ContestParticipation> contests,
		GuildJacobLeaderboard leaderboard,
		GuildJacobLeaderboardFeature feature,
		string playerUuid,
		long profileLastUpdated) {
		
		return contests.Where(cp => {
			var timestamp = cp.JacobContest.Timestamp;

			if (leaderboard.StartCutoff > 0 && timestamp < leaderboard.StartCutoff) return false;
			if (leaderboard.EndCutoff > 0 && leaderboard.EndCutoff != -1 && timestamp > leaderboard.EndCutoff) return false;

			if (feature.ExcludedTimespans.Any(t => t.Start <= timestamp && t.End >= timestamp)) return false;

			var participationKey = $"{timestamp}-{cp.JacobContest.Crop}-{playerUuid}";
			if (feature.ExcludedParticipations.Contains(participationKey)) return false;

			if (profileLastUpdated - timestamp < 22 * 60 && cp.MedalEarned == ContestMedal.None) return false;

			return true;
		}).ToList();
	}

	private static ScoreChangeDto? ProcessContest(
		ContestParticipation contest,
		GuildJacobLeaderboard leaderboard,
		SubmitterInfoDto submitter,
		ulong submitterDiscordId) {
		
		var crop = contest.JacobContest.Crop;
		var cropName = crop.ToString();
		var scores = GetCropScores(leaderboard.Crops, crop);
		
		if (scores is null) return null;

		var collected = contest.Collected;

		if (scores.Count >= 3 && !scores.Any(s => (s.Record.Collected) < collected)) return null;

		var existingEntry = scores.FirstOrDefault(s => s.DiscordId == submitterDiscordId.ToString());
		if (existingEntry is not null && existingEntry.Record.Collected >= collected) return null;

		var isDuplicate = scores.Any(s =>
			s.Record.Collected == collected &&
			s.DiscordId == submitterDiscordId.ToString() &&
			s.Record.Timestamp == contest.JacobContest.Timestamp &&
			s.Record.Crop == cropName);
		if (isDuplicate) return null;

		var oldPosition = existingEntry is not null ? scores.IndexOf(existingEntry) : -1;
		var improvement = existingEntry is not null ? collected - existingEntry.Record.Collected : (int?)null;

		DisplacedEntryDto? knockedOutEntry = null;
		if (scores.Count >= 3 && existingEntry is null) {
			var third = scores[2];
			knockedOutEntry = new DisplacedEntryDto {
				Uuid = third.Uuid,
				Ign = third.Ign,
				DiscordId = third.DiscordId,
				Collected = third.Record.Collected,
				PreviousPosition = 2
			};
		}

		if (existingEntry is not null) {
			scores.Remove(existingEntry);
		}

		var newEntry = new GuildJacobLeaderboardEntry {
			Uuid = submitter.Uuid,
			Ign = submitter.Ign,
			DiscordId = submitter.DiscordId,
			Record = new ContestParticipationDto {
				Crop = cropName,
				Timestamp = contest.JacobContest.Timestamp,
				Collected = contest.Collected,
				Position = contest.Position,
				Participants = contest.JacobContest.Participants,
				Medal = contest.MedalEarned.ToString()
			}
		};

		scores.Add(newEntry);
		scores.Sort((a, b) => b.Record.Collected.CompareTo(a.Record.Collected));

		while (scores.Count > 3) scores.RemoveAt(scores.Count - 1);

		var newPosition = scores.IndexOf(newEntry);

		DisplacedEntryDto? displacedEntry = null;
		if (existingEntry is null && newPosition < scores.Count) {
			var displaced = scores.ElementAtOrDefault(newPosition + 1);
			if (displaced is not null && displaced.Uuid != submitter.Uuid) {
				displacedEntry = new DisplacedEntryDto {
					Uuid = displaced.Uuid,
					Ign = displaced.Ign,
					DiscordId = displaced.DiscordId,
					Collected = displaced.Record.Collected,
					PreviousPosition = newPosition
				};
			}
		}

		return new ScoreChangeDto {
			Crop = cropName,
			NewPosition = newPosition,
			OldPosition = oldPosition,
			Submitter = submitter,
			Record = newEntry.Record,
			DisplacedEntry = displacedEntry,
			KnockedOutEntry = knockedOutEntry,
			Improvement = improvement
		};
	}

	private static List<GuildJacobLeaderboardEntry>? GetCropScores(CropRecords crops, Crop crop) {
		return crop switch {
			Crop.Cactus => crops.Cactus,
			Crop.Carrot => crops.Carrot,
			Crop.CocoaBeans => crops.CocoaBeans,
			Crop.Melon => crops.Melon,
			Crop.Mushroom => crops.Mushroom,
			Crop.NetherWart => crops.NetherWart,
			Crop.Potato => crops.Potato,
			Crop.Pumpkin => crops.Pumpkin,
			Crop.SugarCane => crops.SugarCane,
			Crop.Wheat => crops.Wheat,
			Crop.Sunflower => crops.Sunflower,
			Crop.Moonflower => crops.Moonflower,
			Crop.WildRose => crops.WildRose,
			_ => null
		};
	}
}
