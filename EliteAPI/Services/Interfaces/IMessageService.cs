using EliteAPI.Models.DTOs.Outgoing.Messaging;

namespace EliteAPI.Services.Interfaces;

public interface IMessageService
{
	void SendMessage(MessageDto messageDto);
	void SendErrorMessage(string title, string message);
	void SendPurchaseMessage(string accountId, string skuId, string skuName = "Unknown");
	void SendClaimMessage(string accountId, string skuId, string skuName = "Unknown");
	void SendWipedMessage(string uuid, string ign, string profileId, string discordId = "");
}