using System.Text;
using System.Text.Json;
using EliteAPI.Models.DTOs.Outgoing.Messaging;
using RabbitMQ.Client;

namespace EliteAPI.Services.MessageService; 

public class MessageService : IMessageService {

    private const string ExchangeName = "eliteapi";
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    
    public MessageService() {
        var factory = new ConnectionFactory {
            HostName = "localhost",
            UserName = "user",
            Password = "rabbitPassword123"
        };

        try {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        } catch (Exception e) {
            Console.Error.WriteLine(e);
        }
        
        _channel?.ExchangeDeclare(ExchangeName, ExchangeType.Fanout);
    }

    ~MessageService() {
        _channel?.Close();
        _connection?.Close();
    }

    public void SendMessage(MessageDto messageDto) {
        if (_channel is null || !_channel.IsOpen) return;
        
        var message = JsonSerializer.Serialize(messageDto, new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var body = Encoding.UTF8.GetBytes(message);
        
        _channel.BasicPublish(ExchangeName, string.Empty, null, body);
    }
}