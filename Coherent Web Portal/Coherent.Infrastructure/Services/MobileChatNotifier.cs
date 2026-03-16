using Coherent.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Coherent.Infrastructure.Services;

/// <summary>
/// Calls Mobile Backend's internal endpoint to broadcast a chat message
/// via its ChatHub to all connected clients (Flutter app + Angular CRM UI).
/// Fire-and-forget style — failures are logged but don't block the API response.
/// </summary>
public class MobileChatNotifier : IMobileChatNotifier
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MobileChatNotifier> _logger;

    public MobileChatNotifier(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MobileChatNotifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task NotifyMessageAsync(int conversationId, int messageId, string? senderType,
        string? senderName, string? messageType, string? content,
        string? fileUrl, string? fileName, long? fileSize,
        DateTime sentAt, string? crmMessageId, string? crmThreadId)
    {
        var baseUrl = _configuration["MobileBackend:BaseUrl"];
        var apiKey = _configuration["MobileBackend:InternalApiKey"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("[MobileChatNotifier] MobileBackend:BaseUrl or InternalApiKey not configured — skipping notification");
            return;
        }

        try
        {
            var payload = new
            {
                messageId,
                conversationId,
                senderId = 0,
                senderType,
                senderName,
                messageType,
                content,
                fileUrl,
                fileName,
                fileSize,
                sentAt,
                crmMessageId,
                crmThreadId
            };

            var json = JsonSerializer.Serialize(payload);
            var client = _httpClientFactory.CreateClient("MobileBackend");

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/internal/chat-notify/broadcast");
            request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[MobileChatNotifier] Notify failed — {StatusCode} for message {MessageId}",
                    (int)response.StatusCode, messageId);
            }
            else
            {
                _logger.LogInformation("[MobileChatNotifier] Broadcast notification sent for message {MessageId} in conversation {ConversationId}",
                    messageId, conversationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MobileChatNotifier] Failed to notify Mobile Backend for message {MessageId}", messageId);
        }
    }
}
