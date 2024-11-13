using System.Text;
using System.Text.Json;
using EliteAPI.Configuration.Settings;
using EliteAPI.Models.DTOs.Outgoing.Messaging;
using EliteAPI.Services.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EliteAPI.Services; 

public class MessageService : IMessageService {

    private const string ExchangeName = "eliteapi";
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly RabbitMqSettings _rabbitMqSettings;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public MessageService(IOptions<RabbitMqSettings> rabbitMqSettings) {
        _rabbitMqSettings = rabbitMqSettings.Value;
        
        var factory = new ConnectionFactory {
            HostName = _rabbitMqSettings.Host,
            UserName = _rabbitMqSettings.User,
            Password = _rabbitMqSettings.Password,
            Port = _rabbitMqSettings.Port
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
        
        var message = JsonSerializer.Serialize(messageDto, _jsonSerializerOptions);
        var body = Encoding.UTF8.GetBytes(message);
        
        _channel.BasicPublish(ExchangeName, string.Empty, null, body);
    }
    
    public void SendErrorMessage(string title, string message) {
        if (string.IsNullOrEmpty(_rabbitMqSettings.ErrorAlertServer) || string.IsNullOrEmpty(_rabbitMqSettings.ErrorAlertChannel)) return;
        
        SendMessage(new MessageDto {
            Name = "error",
            GuildId = _rabbitMqSettings.ErrorAlertServer,
            AuthorId = _rabbitMqSettings.ErrorAlertChannel,
            Data = $$"""
            {
                "channelId": "{{_rabbitMqSettings.ErrorAlertChannel}}",
                "title": "{{title}}",
                "message": "{{message}}",
                "ping": "{{_rabbitMqSettings.ErrorAlertPing}}"
            }
            """
        });
    }

    public void SendPurchaseMessage(string accountId, string skuId, string skuName) {
        if (string.IsNullOrEmpty(_rabbitMqSettings.ErrorAlertServer)) return;
        
        SendMessage(new MessageDto {
            Name = "purchase",
            GuildId = _rabbitMqSettings.ErrorAlertServer,
            AuthorId = accountId,
            Data = $$"""
            {
                "userId": "{{accountId}}",
                "skuId": "{{skuId}}",
                "skuName": "{{skuName}}"
            }
            """
        });
    }
    
    public void SendWipedMessage(string uuid, string ign, string profileId, string discordId) {
        if (string.IsNullOrEmpty(_rabbitMqSettings.WipeServer) || string.IsNullOrEmpty(_rabbitMqSettings.WipeChannel)) return;
        
        SendMessage(new MessageDto {
            Name = "wipe",
            GuildId = _rabbitMqSettings.WipeServer,
            AuthorId = _rabbitMqSettings.WipeChannel,
            Data = $$"""
                     {
                         "channelId": "{{_rabbitMqSettings.WipeChannel}}",
                         "uuid": "{{uuid}}",
                         "ign": "{{ign}}",
                         "profileId": "{{profileId}}",
                         "discord": "{{discordId}}"
                     }
                     """
        });
    }
}