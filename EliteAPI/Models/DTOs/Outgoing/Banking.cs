using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.DTOs.Outgoing;

public class ProfileBankingDto
{
	public double Balance { get; set; } = 0;
	public List<ProfileBankingTransactionDto> Transactions { get; set; } = new();
}

public class ProfileBankingTransactionDto
{
	public double Amount { get; set; } = 0;
	public BankingTransactionAction Action { get; set; }
	public DateTime Timestamp { get; set; }
	public string? Initiator { get; set; }
}