namespace EliteAPI.Models.Entities.Hypixel;

public class ProfileBanking {
	public double Balance { get; set; } = 0;

	public List<ProfileBankingTransaction> Transactions { get; set; } = new();
}

public class ProfileBankingTransaction {
	public double Amount { get; set; } = 0;
	public BankingTransactionAction Action { get; set; }
	public DateTime Timestamp { get; set; }
	public string? Initiator { get; set; }
}

public enum BankingTransactionAction {
	Withdraw,
	Deposit
}