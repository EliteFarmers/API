using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Utilities;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Profiles.Services;

public interface IContestsProcessorService
{
	Task ProcessContests(Guid memberId, RawJacobData? incoming);
	Task ProcessContests(JacobData jacobData, RawJacobData? incomingData);
}

[RegisterService<IContestsProcessorService>(LifeTime.Scoped)]
public class ContestsProcessorService(
	DataContext context,
	ILogger<ContestsProcessorService> logger)
	: IContestsProcessorService
{
	public async Task ProcessContests(Guid memberId, RawJacobData? incoming) {
		if (incoming is null) return;

		var jacob = await context.JacobData
			.Include(j => j.Contests)
			.ThenInclude(c => c.JacobContest)
			.FirstOrDefaultAsync(j => j.ProfileMemberId == memberId);

		if (jacob is null) return;

		await ProcessContests(jacob, incoming);
	}

	public async Task ProcessContests(JacobData jacob, RawJacobData? incoming) {
		if (incoming is null) return;

		var memberId = jacob.ProfileMemberId;

		var incomingContests = incoming.Contests;
		if (incomingContests.Count == 0) return;

		// If the last update was 0, we need to do a full refresh
		// This can be triggered manually to correct for errors overtime
		var fullRefresh = jacob.ContestsLastUpdated == 0;

		var claimedDictionary = new Dictionary<long, bool>();

		await using var transaction = await context.Database.BeginTransactionAsync();

		// Existing contests on the profile
		var existingContests = jacob.Contests
			.DistinctBy(c => c.JacobContest.Timestamp + (int)c.JacobContest.Crop) // Hopefully should not be needed
			.ToDictionary(c => c.JacobContest.Timestamp + (int)c.JacobContest.Crop);

		// List of keys to fetch
		var contestsToFetch = incomingContests
			.Where(c => {
				if (c.Value.Collected < 100)
					return false; // Contests with less than 100 crops collected aren't counted in game

				var timestamp = FormatUtils.GetTimeFromContestKey(c.Key);
				var actualKey = timestamp + (int)(FormatUtils.GetCropFromContestKey(c.Key) ?? 0);
				if (c.Value.Position is not null) {
					claimedDictionary[timestamp] = true;
				}
				else {
					claimedDictionary[timestamp] = claimedDictionary.GetValueOrDefault(timestamp);
				}

				if (fullRefresh) return true;

				// If the contest is new, add it to the list
				if (!existingContests.TryGetValue(actualKey, out var existing)) return true;

				// Add if it hasn't been claimed
				return existing.Position <= 0;
			}).ToList();

		var keys = contestsToFetch.Select(c =>
			FormatUtils.GetTimeFromContestKey(c.Key) + (int)(FormatUtils.GetCropFromContestKey(c.Key) ?? 0)
		).ToList();

		// Fetch contests from database
		var fetchedContests = new Dictionary<long, JacobContest>();

		try {
			fetchedContests = await context.JacobContests
				.Where(j => keys.Contains(j.Id))
				.ToDictionaryAsync(j => j.Id);
		}
		catch (Exception e) {
			logger.LogError(e, "Failed to fetch contests from database");
		}

		var newParticipations = new List<ContestParticipation>();
		foreach (var (key, contest) in contestsToFetch) {
			if (contest.Collected < 100) continue; // Contests with less than 100 crops collected aren't counted in game
			jacob.Participations++;

			var medal = contest.ExtractMedal();

			var crop = FormatUtils.GetCropFromContestKey(key);
			if (crop is null) continue;

			var timestamp = FormatUtils.GetTimeFromContestKey(key);
			var actualKey = timestamp + (int)crop;

			// If this participation has no medal, and we already have a claimed contest at this timestamp
			// Set the medal to unclaimable (this is a duplicate contest)
			if (medal == ContestMedal.None && contest.Position is null && claimedDictionary.ContainsKey(timestamp)) {
				medal = ContestMedal.Unclaimable;
			}

			if (!fetchedContests.TryGetValue(actualKey, out var fetched)) {
				var newContest = new JacobContest {
					Id = actualKey,
					Crop = crop.GetValueOrDefault(),
					Timestamp = timestamp,
					Participants = contest.Participants ?? -1,
					Finnegan = contest.Medal is not null // Only Finnegan contests have a medal set
				};

				try {
					context.JacobContests.Add(newContest);
				}
				catch (Exception e) {
					logger.LogError(e, "Failed to add new contest to database");
					continue;
				}

				fetchedContests.Add(actualKey, newContest);
				fetched = newContest;
			}

			// Update the number of participants if it's not set
			// Should happen infrequently
			if (fetched.Participants == -1 && contest.Participants > 0) {
				if (contest.Medal is not null) fetched.Finnegan = true;

				await context.JacobContests
					.Where(j => j.Id == actualKey)
					.ExecuteUpdateAsync(j =>
						j.SetProperty(c => c.Participants, contest.Participants ?? -1)
							.SetProperty(c => c.Finnegan, fetched.Finnegan));
			}

			if (!existingContests.TryGetValue(actualKey, out var existing)) {
				// Register new participation
				var newParticipation = new ContestParticipation {
					Collected = contest.Collected,
					Position = contest.Position ?? -1,
					MedalEarned = medal,

					ProfileMemberId = memberId,
					JacobContestId = actualKey
				};

				fetched.UpdateMedalBracket(newParticipation);
				newParticipations.Add(newParticipation);
			}
			else if (
				existing.Collected != contest.Collected
				|| (contest.Position is not null && existing.Position != contest.Position)
				|| existing.MedalEarned != medal) {
				await context.ContestParticipations
					.Where(p => p.ProfileMemberId == memberId && p.JacobContestId == actualKey)
					.ExecuteUpdateAsync(p => p
						.SetProperty(c => c.Collected, contest.Collected)
						.SetProperty(c => c.Position, contest.Position ?? -1)
						.SetProperty(c => c.MedalEarned, medal));

				fetched.UpdateMedalBracket(existing);
			}
		}

		context.ContestParticipations.AddRange(newParticipations);
		jacob.Contests.AddRange(newParticipations);

		var medalCounts = await context.JacobData
			.Where(p => p.Id == jacob.Id)
			.SelectMany(p => p.Contests)
			.GroupBy(x => x.MedalEarned)
			.Select(p => new {
				Medal = p.Key,
				Count = p.Count()
			}).ToDictionaryAsync(k => k.Medal, v => v.Count);

		jacob.Participations = jacob.Contests.Count;

		jacob.EarnedMedals.Bronze = medalCounts.GetValueOrDefault(ContestMedal.Bronze, 0);
		jacob.EarnedMedals.Silver = medalCounts.GetValueOrDefault(ContestMedal.Silver, 0);
		jacob.EarnedMedals.Gold = medalCounts.GetValueOrDefault(ContestMedal.Gold, 0);
		jacob.EarnedMedals.Platinum = medalCounts.GetValueOrDefault(ContestMedal.Platinum, 0);
		jacob.EarnedMedals.Diamond = medalCounts.GetValueOrDefault(ContestMedal.Diamond, 0);

		jacob.FirstPlaceScores = jacob.Contests.Count(c => c.Position == 0);

		jacob.Stats ??= new JacobStats();
		jacob.Stats.Crops = new Dictionary<Crop, JacobCropStats> {
			{ Crop.Cactus, new JacobCropStats() },
			{ Crop.Carrot, new JacobCropStats() },
			{ Crop.CocoaBeans, new JacobCropStats() },
			{ Crop.Melon, new JacobCropStats() },
			{ Crop.Mushroom, new JacobCropStats() },
			{ Crop.NetherWart, new JacobCropStats() },
			{ Crop.Potato, new JacobCropStats() },
			{ Crop.Pumpkin, new JacobCropStats() },
			{ Crop.SugarCane, new JacobCropStats() },
			{ Crop.Wheat, new JacobCropStats() }
		};

		foreach (var contest in jacob.Contests) {
			if (contest.MedalEarned == ContestMedal.Unclaimable) continue;
			if (!jacob.Stats.Crops.TryGetValue(contest.JacobContest.Crop, out var cropStats)) continue;

			cropStats.Participations++;
			if (contest.Position == 0) cropStats.FirstPlaceScores++;
			cropStats.Medals.AddMedal(contest);

			if (contest.Collected == jacob.Stats.PersonalBests.GetValueOrDefault(contest.JacobContest.Crop, -2))
				cropStats.PersonalBestTimestamp = contest.JacobContest.Timestamp;
		}

		context.Entry(jacob).Property(j => j.Stats).IsModified = true;

		jacob.ContestsLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

		if (context.Entry(jacob).State == EntityState.Detached) {
			context.Attach(jacob);
			context.Entry(jacob).State = EntityState.Modified;
		}

		await context.SaveChangesAsync();
		await transaction.CommitAsync();
	}
}