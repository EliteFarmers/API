using EliteAPI.Models.DTOs.Outgoing.Messaging;

namespace EliteAPI.Services.MessageService; 

public interface IMessageService {
    void SendMessage(MessageDto messageDto);
    void SendErrorMessage(string title, string message);
}