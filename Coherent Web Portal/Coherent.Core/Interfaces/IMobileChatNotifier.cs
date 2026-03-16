namespace Coherent.Core.Interfaces;

/// <summary>
/// Notifies the Mobile Backend's ChatHub to broadcast a message to connected clients.
/// Used when a message is sent via Web Backend REST API — ensures both
/// Angular CRM UI and Flutter app receive real-time notifications.
/// </summary>
public interface IMobileChatNotifier
{
    Task NotifyMessageAsync(int conversationId, int messageId, string? senderType,
        string? senderName, string? messageType, string? content,
        string? fileUrl, string? fileName, long? fileSize,
        DateTime sentAt, string? crmMessageId, string? crmThreadId);
}
