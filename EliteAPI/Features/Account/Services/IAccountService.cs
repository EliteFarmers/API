using EliteAPI.Features.Account.DTOs;
using EliteAPI.Features.Account.Models;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Account.Services;

public interface IAccountService
{
    /// <summary>
    /// Gets an account by the linked Discord account Id
    /// </summary>
    /// <param name="accountId">Linked Discord account Id</param>
    /// <returns></returns>
    Task<EliteAccount?> GetAccount(ulong accountId);
    Task<EliteAccount?> GetAccountByIgn(string ign);
    Task<EliteAccount?> GetAccountByMinecraftUuid(string uuid);
    Task<EliteAccount?> GetAccountByIgnOrUuid(string ignOrUuid);
    Task<ErrorOr<Success>> LinkAccount(ulong discordId, string playerUuidOrIgn);
    Task<ErrorOr<Success>> UnlinkAccount(ulong discordId, string playerUuidOrIgn);
    Task<ErrorOr<Success>> MakePrimaryAccount(ulong discordId, string playerUuidOrIgn);
    Task<ErrorOr<Success>> UpdateSettings(ulong discordId, UpdateUserSettingsDto settings);
    Task<ErrorOr<Success>> UpdateFortuneSettings(ulong discordId, string playerUuid, string profileUuid, MemberFortuneSettingsDto settings);
}
