using EliteAPI.Models.DTOs.Outgoing.Messaging;
using EliteAPI.Models.Entities.Monetization;

namespace EliteAPI.Services.Interfaces; 

public interface IMessageService {
    void SendMessage(MessageDto messageDto);
    void SendErrorMessage(string title, string message);
    void SendPurchaseMessage(Entitlement entitlement);
}