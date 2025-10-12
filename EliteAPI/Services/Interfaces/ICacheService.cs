namespace EliteAPI.Services.Interfaces;

public interface ICacheService {
	public Task<string?> GetUsernameFromUuid(string uuid);
	public Task<string?> GetUuidFromUsername(string username);
	public void SetUsernameUuidCombo(string username, string uuid, TimeSpan? expiry = null);
	public Task<bool> IsContestUpdateRequired(long contestId);
	public void SetContest(long contestId, bool claimed = false);
}