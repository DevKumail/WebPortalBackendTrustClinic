using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using Coherent.Web.Portal.Hubs;

namespace Coherent.Web.Portal.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/chat")]
[ApiVersion("2.0")]
[ThirdPartyAuth]
public class ChatController : ControllerBase
{
    private readonly IChatRepository _chatRepository;
    private readonly IChatWebhookOutboxRepository _outbox;
    private readonly IHubContext<CrmChatHub> _hub;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatRepository chatRepository, IChatWebhookOutboxRepository outbox, IHubContext<CrmChatHub> hub, ILogger<ChatController> logger)
    {
        _chatRepository = chatRepository;
        _outbox = outbox;
        _hub = hub;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("threads/get-or-create")]
    [ProducesResponseType(typeof(ChatThreadGetOrCreateResponse), 200)]
    public async Task<IActionResult> GetOrCreateThread([FromBody] ChatThreadGetOrCreateRequest request)
    {
        var result = await _chatRepository.GetOrCreateThreadAsync(request);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("messages")]
    [ProducesResponseType(typeof(ChatSendMessageResponse), 200)]
    public async Task<IActionResult> SendMessage([FromBody] ChatSendMessageRequest request)
    {
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
                // Parse thread id back for payload
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

    [AllowAnonymous]
    [HttpGet("messages/updates")]
    [ProducesResponseType(typeof(List<ChatMessageUpdateResponse>), 200)]
    public async Task<IActionResult> GetUpdates([FromQuery] DateTime since, [FromQuery] int limit = 100)
    {
        var updates = await _chatRepository.GetDoctorToPatientUpdatesAsync(since.ToUniversalTime(), limit);
        return Ok(updates);
    }
}
