using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Hypixel;

public class ProfileBanking
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public double Balance { get; set; } = 0;

    [Column(TypeName = "jsonb")]
    public List<ProfileBankingTransaction> Transactions { get; set; } = new();
}

public class ProfileBankingTransaction
{
    public double Amount { get; set; } = 0;
    public BankingTransactionAction Action { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Initiator { get; set; }
}

public enum BankingTransactionAction
{
    Withdraw,
    Deposit
}