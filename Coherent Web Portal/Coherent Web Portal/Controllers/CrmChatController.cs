using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Web.Portal.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/crm-chat")]
[ApiVersion("1.0")]
[Authorize]
public class CrmChatController : ControllerBase
{
    private readonly IChatRepository _chatRepository;
    private readonly IChatWebhookOutboxRepository _outbox;
    private readonly IHubContext<CrmChatHub> _hub;
    private readonly ILogger<CrmChatController> _logger;

    public CrmChatController(
        IChatRepository chatRepository,
        IChatWebhookOutboxRepository outbox,
        IHubContext<CrmChatHub> hub,
        ILogger<CrmChatController> logger)
    {
        _chatRepository = chatRepository;
        _outbox = outbox;
        _hub = hub;
        _logger = logger;
    }

    [HttpPost("threads/get-or-create")]
    [ProducesResponseType(typeof(ChatThreadGetOrCreateResponse), 200)]
    public async Task<IActionResult> GetOrCreateThread([FromBody] ChatThreadGetOrCreateRequest request)
    {
        var result = await _chatRepository.GetOrCreateThreadAsync(request);
        return Ok(result);
    }

    [HttpPost("messages")]
    [ProducesResponseType(typeof(ChatSendMessageResponse), 200)]
    public async Task<IActionResult> SendDoctorMessage([FromBody] ChatSendMessageRequest request)
    {
        if (request.ClientMessageId == Guid.Empty)
            request.ClientMessageId = Guid.NewGuid();

        request.SenderType = "Doctor";
        request.ReceiverType = "Patient";

        var (response, isDoctorToPatient) = await _chatRepository.SendMessageAsync(request);

        try
        {
            await _hub.Clients.Group(request.CrmThreadId).SendAsync("chat.message.created", new
            {
                crmThreadId = request.CrmThreadId,
                crmMessageId = response.CrmMessageId,
                senderType = request.SenderType,
                senderMrNo = request.SenderMrNo,
                senderDoctorLicenseNo = request.SenderDoctorLicenseNo,
                receiverType = request.ReceiverType,
                receiverMrNo = request.ReceiverMrNo,
                receiverDoctorLicenseNo = request.ReceiverDoctorLicenseNo,
                messageType = request.MessageType,
                content = request.Content,
                fileUrl = request.FileUrl,
                fileName = request.FileName,
                fileSize = request.FileSize,
                sentAt = request.SentAt == default ? DateTime.UtcNow : request.SentAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast chat message via SignalR");
        }

        if (isDoctorToPatient)
        {
            try
            {
                var conversationId = request.CrmThreadId.StartsWith("CRM-TH-", StringComparison.OrdinalIgnoreCase)
                    ? request.CrmThreadId.Substring("CRM-TH-".Length)
                    : request.CrmThreadId;

                var webhookPayload = new ChatDoctorMessageCreatedWebhook
                {
                    CrmThreadId = $"CRM-TH-{conversationId}",
                    CrmMessageId = response.CrmMessageId,
                    DoctorLicenseNo = request.SenderDoctorLicenseNo ?? string.Empty,
                    PatientMrNo = request.ReceiverMrNo ?? string.Empty,
                    MessageType = request.MessageType,
                    Content = request.Content,
                    FileUrl = request.FileUrl,
                    FileName = request.FileName,
                    FileSize = request.FileSize,
                    SentAt = request.SentAt == default ? DateTime.UtcNow : request.SentAt
                };

                var payloadJson = JsonSerializer.Serialize(webhookPayload);

                await _outbox.EnqueueIfNotExistsAsync(
                    response.CrmMessageId,
                    webhookPayload.CrmThreadId,
                    webhookPayload.DoctorLicenseNo,
                    webhookPayload.PatientMrNo,
                    payloadJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue chat webhook outbox");
            }
        }

        return Ok(response);
    }

    [HttpGet("unread-summary")]
    [ProducesResponseType(typeof(ChatDoctorUnreadSummaryResponse), 200)]
    public async Task<IActionResult> GetUnreadSummary([FromQuery] string doctorLicenseNo, [FromQuery] int limit = 50)
    {
        var result = await _chatRepository.GetDoctorUnreadSummaryAsync(doctorLicenseNo, limit);
        return Ok(result);
    }

    [HttpGet("conversations")]
    [ProducesResponseType(typeof(ChatConversationListResponse), 200)]
    public async Task<IActionResult> GetConversations([FromQuery] string? doctorLicenseNo, [FromQuery] string? patientMrNo, [FromQuery] int limit = 50)
    {
        var result = await _chatRepository.GetConversationListAsync(doctorLicenseNo, patientMrNo, limit);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        foreach (var item in result.Conversations)
        {
            var photo = item.Counterpart.DoctorPhotoName;
            if (!string.IsNullOrWhiteSpace(photo) && !Uri.TryCreate(photo, UriKind.Absolute, out _))
            {
                item.Counterpart.DoctorPhotoName = $"{baseUrl}/images/doctors/{photo.TrimStart('/')}";
            }
        }

        return Ok(result);
    }

    [HttpGet("threads/{crmThreadId}/messages")]
    [ProducesResponseType(typeof(List<ChatThreadMessageDto>), 200)]
    public async Task<IActionResult> GetThreadMessages([FromRoute] string crmThreadId, [FromQuery] int take = 50)
    {
        var messages = await _chatRepository.GetThreadMessagesAsync(crmThreadId, take);
        return Ok(messages);
    }

    [HttpPost("threads/{crmThreadId}/mark-read")]
    [ProducesResponseType(typeof(ChatMarkReadResponse), 200)]
    public async Task<IActionResult> MarkThreadRead([FromRoute] string crmThreadId, [FromQuery] string doctorLicenseNo)
    {
        var result = await _chatRepository.MarkThreadAsReadAsync(crmThreadId, doctorLicenseNo);
        return Ok(result);
    }
}
