using System.Text.Json;
using EliteAPI.Configuration.Settings;
using EliteAPI.Models.DTOs.Outgoing.Messaging;
using EliteAPI.Services.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services;

public class MessageService(
	IConnectionMultiplexer redis,
	IOptions<MessagingSettings> messagingSettings,
	ILogger<MessageService> logger
) : IMessageService
{
	private const string RedisChannelName = "eliteapi_messages";
	private readonly MessagingSettings _messagingSettings = messagingSettings.Value;

	private readonly JsonSerializerOptions _jsonSerializerOptions = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public void SendMessage(MessageDto messageDto) {
		if (!redis.IsConnected) {
			logger.LogWarning("Cannot send message. Redis is not connected");
			return;
		}

		try {
			var subscriber = redis.GetSubscriber();
			var message = JsonSerializer.Serialize(messageDto, _jsonSerializerOptions);

			var redisChannel = new RedisChannel(RedisChannelName, RedisChannel.PatternMode.Literal);
			var clientsReceived = subscriber.Publish(redisChannel, message, CommandFlags.FireAndForget);

			logger.LogDebug("Published message to Redis channel '{ChannelName}'. Received by {ClientCount} clients",
				RedisChannelName, clientsReceived);
		}
		catch (Exception ex) {
			logger.LogError(ex, "Error publishing message to Redis channel '{ChannelName}'", RedisChannelName);
		}
	}

	public void SendErrorMessage(string title, string message) {
		if (string.IsNullOrEmpty(_messagingSettings.ErrorAlertServer) ||
		    string.IsNullOrEmpty(_messagingSettings.ErrorAlertChannel)) return;

		SendMessage(new MessageDto {
			Name = "error",
			GuildId = _messagingSettings.ErrorAlertServer,
			AuthorId = _messagingSettings.ErrorAlertChannel,
			Data = new Dictionary<string, object> {
				{ "channelId", _messagingSettings.ErrorAlertChannel },
				{ "title", title },
				{ "message", message },
				{ "ping", _messagingSettings.ErrorAlertPing ?? "" }
			}
		});
	}

	public void SendPurchaseMessage(string accountId, string skuId, string skuName) {
		if (string.IsNullOrEmpty(_messagingSettings.ErrorAlertServer)) return;

		SendMessage(new MessageDto {
			Name = "purchase",
			GuildId = _messagingSettings.ErrorAlertServer,
			AuthorId = accountId,
			Data = new Dictionary<string, object> {
				{ "userId", accountId },
				{ "skuId", skuId },
				{ "skuName", skuName }
			}
		});
	}

	public void SendClaimMessage(string accountId, string skuId, string skuName) {
		if (string.IsNullOrEmpty(_messagingSettings.ErrorAlertServer)) return;

		SendMessage(new MessageDto {
			Name = "claim",
			GuildId = _messagingSettings.ErrorAlertServer,
			AuthorId = accountId,
			Data = new Dictionary<string, object> {
				{ "userId", accountId },
				{ "skuId", skuId },
				{ "skuName", skuName }
			}
		});
	}

	public void SendWipedMessage(string uuid, string ign, string profileId, string discordId) {
		if (string.IsNullOrEmpty(_messagingSettings.WipeServer) ||
		    string.IsNullOrEmpty(_messagingSettings.WipeChannel)) return;

		SendMessage(new MessageDto {
			Name = "wipe",
			GuildId = _messagingSettings.WipeServer,
			AuthorId = _messagingSettings.WipeChannel,
			Data = new Dictionary<string, object> {
				{ "channelId", _messagingSettings.WipeChannel },
				{ "uuid", uuid },
				{ "ign", ign },
				{ "profileId", profileId },
				{ "discord", discordId }
			}
		});
	}
}