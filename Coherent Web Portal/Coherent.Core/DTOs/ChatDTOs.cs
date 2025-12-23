using System.Text.Json.Serialization;

namespace Coherent.Core.DTOs;

public class ChatThreadGetOrCreateRequest
{
    [JsonPropertyName("patientMrNo")]
    public string PatientMrNo { get; set; } = string.Empty;

    [JsonPropertyName("doctorLicenseNo")]
    public string DoctorLicenseNo { get; set; } = string.Empty;

    [JsonPropertyName("sourceSystem")]
    public string? SourceSystem { get; set; }
}

public class ChatThreadGetOrCreateResponse
{
    [JsonPropertyName("crmThreadId")]
    public string CrmThreadId { get; set; } = string.Empty;

    [JsonPropertyName("patientMrNo")]
    public string PatientMrNo { get; set; } = string.Empty;

    [JsonPropertyName("doctorLicenseNo")]
    public string DoctorLicenseNo { get; set; } = string.Empty;
}

public class ChatSendMessageRequest
{
    [JsonPropertyName("crmThreadId")]
    public string CrmThreadId { get; set; } = string.Empty;

    [JsonPropertyName("senderType")]
    public string SenderType { get; set; } = string.Empty;

    [JsonPropertyName("senderMrNo")]
    public string? SenderMrNo { get; set; }

    [JsonPropertyName("senderDoctorLicenseNo")]
    public string? SenderDoctorLicenseNo { get; set; }

    [JsonPropertyName("receiverType")]
    public string ReceiverType { get; set; } = string.Empty;

    [JsonPropertyName("receiverMrNo")]
    public string? ReceiverMrNo { get; set; }

    [JsonPropertyName("receiverDoctorLicenseNo")]
    public string? ReceiverDoctorLicenseNo { get; set; }

    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = "Text";

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("fileUrl")]
    public string? FileUrl { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileSize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("clientMessageId")]
    public Guid ClientMessageId { get; set; }

    [JsonPropertyName("sentAt")]
    public DateTime SentAt { get; set; }
}

public class ChatConversationCounterpartDto
{
    [JsonPropertyName("userType")]
    public string UserType { get; set; } = string.Empty;

    [JsonPropertyName("doctorLicenseNo")]
    public string? DoctorLicenseNo { get; set; }

    [JsonPropertyName("doctorName")]
    public string? DoctorName { get; set; }

    [JsonPropertyName("doctorTitle")]
    public string? DoctorTitle { get; set; }

    [JsonPropertyName("doctorPhotoName")]
    public string? DoctorPhotoName { get; set; }

    [JsonPropertyName("patientMrNo")]
    public string? PatientMrNo { get; set; }

    [JsonPropertyName("patientName")]
    public string? PatientName { get; set; }
}

public class ChatConversationListItemDto
{
    [JsonPropertyName("crmThreadId")]
    public string CrmThreadId { get; set; } = string.Empty;

    [JsonPropertyName("lastMessageAt")]
    public DateTime? LastMessageAt { get; set; }

    [JsonPropertyName("lastMessagePreview")]
    public string? LastMessagePreview { get; set; }

    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("counterpart")]
    public ChatConversationCounterpartDto Counterpart { get; set; } = new();
}

public class ChatConversationListResponse
{
    [JsonPropertyName("doctorLicenseNo")]
    public string? DoctorLicenseNo { get; set; }

    [JsonPropertyName("patientMrNo")]
    public string? PatientMrNo { get; set; }

    [JsonPropertyName("serverTimeUtc")]
    public DateTime ServerTimeUtc { get; set; }

    [JsonPropertyName("conversations")]
    public List<ChatConversationListItemDto> Conversations { get; set; } = new();
}

public class ChatUnreadThreadItem
{
    [JsonPropertyName("crmThreadId")]
    public string CrmThreadId { get; set; } = string.Empty;

    [JsonPropertyName("patientMrNo")]
    public string PatientMrNo { get; set; } = string.Empty;

    [JsonPropertyName("patientName")]
    public string PatientName { get; set; } = string.Empty;

    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("lastMessageAt")]
    public DateTime LastMessageAt { get; set; }

    [JsonPropertyName("lastMessagePreview")]
    public string LastMessagePreview { get; set; } = string.Empty;
}

public class ChatDoctorUnreadSummaryResponse
{
    [JsonPropertyName("doctorLicenseNo")]
    public string DoctorLicenseNo { get; set; } = string.Empty;

    [JsonPropertyName("totalUnread")]
    public int TotalUnread { get; set; }

    [JsonPropertyName("threads")]
    public List<ChatUnreadThreadItem> Threads { get; set; } = new();
}

public class ChatThreadMessageDto
{
    [JsonPropertyName("crmMessageId")]
    public string CrmMessageId { get; set; } = string.Empty;

    [JsonPropertyName("crmThreadId")]
    public string CrmThreadId { get; set; } = string.Empty;

    [JsonPropertyName("senderType")]
    public string SenderType { get; set; } = string.Empty;

    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = "Text";

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("fileUrl")]
    public string? FileUrl { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileSize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("sentAt")]
    public DateTime SentAt { get; set; }
}

public class ChatMarkReadResponse
{
    [JsonPropertyName("crmThreadId")]
    public string CrmThreadId { get; set; } = string.Empty;

    [JsonPropertyName("markedReadCount")]
    public int MarkedReadCount { get; set; }

    [JsonPropertyName("serverProcessedAt")]
    public DateTime ServerProcessedAt { get; set; }
}

public class ChatSendMessageResponse
{
    [JsonPropertyName("crmMessageId")]
    public string CrmMessageId { get; set; } = string.Empty;

    [JsonPropertyName("crmThreadId")]
    public string CrmThreadId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Accepted";

    [JsonPropertyName("serverReceivedAt")]
    public DateTime ServerReceivedAt { get; set; }
}

public class ChatDoctorMessageCreatedWebhook
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = "DoctorMessageCreated";

    [JsonPropertyName("crmThreadId")]
    public string CrmThreadId { get; set; } = string.Empty;

    [JsonPropertyName("crmMessageId")]
    public string CrmMessageId { get; set; } = string.Empty;

    [JsonPropertyName("doctorLicenseNo")]
    public string DoctorLicenseNo { get; set; } = string.Empty;

    [JsonPropertyName("patientMrNo")]
    public string PatientMrNo { get; set; } = string.Empty;

    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = "Text";

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("fileUrl")]
    public string? FileUrl { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileSize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("sentAt")]
    public DateTime SentAt { get; set; }
}

public class ChatMessageUpdateResponse
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = "DoctorMessageCreated";

    [JsonPropertyName("crmThreadId")]
    public string CrmThreadId { get; set; } = string.Empty;

    [JsonPropertyName("crmMessageId")]
    public string CrmMessageId { get; set; } = string.Empty;

    [JsonPropertyName("doctorLicenseNo")]
    public string DoctorLicenseNo { get; set; } = string.Empty;

    [JsonPropertyName("patientMrNo")]
    public string PatientMrNo { get; set; } = string.Empty;

    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = "Text";

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("fileUrl")]
    public string? FileUrl { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileSize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("sentAt")]
    public DateTime SentAt { get; set; }
}
