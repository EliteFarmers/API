using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Data.Models.Hypixel;

public class ProfileBanking
{
    [Key] public int Id { get; set; }
    public double Balance { get; set; } = 0;
    public List<ProfileBankingTransaction> Transactions { get; set; } = new();
}

public class ProfileBankingTransaction
{
    [Key] public int Id { get; set; }
    public double Amount { get; set; } = 0;
    public BankingTransactionAction Action { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Initiator { get; set; }

    [ForeignKey("ProfileBanking")]
    public int ProfileBankingId { get; set; }
    public required ProfileBanking ProfileBanking { get; set; }
}

public enum BankingTransactionAction
{
    Withdraw,
    Deposit
}