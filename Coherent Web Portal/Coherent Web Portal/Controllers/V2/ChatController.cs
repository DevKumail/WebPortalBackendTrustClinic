using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Coherent.Web.Portal.Hubs;

namespace Coherent.Web.Portal.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/chat")]
[ApiVersion("2.0")]
[ThirdPartyAuth]
public class ChatController : ControllerBase
{
    private readonly IChatRepository _chatRepository;
    private readonly IHubContext<CrmChatHub> _hub;
    private readonly IMobileChatNotifier _mobileChatNotifier;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatRepository chatRepository, IHubContext<CrmChatHub> hub, IMobileChatNotifier mobileChatNotifier, ILogger<ChatController> logger)
    {
        _chatRepository = chatRepository;
        _hub = hub;
        _mobileChatNotifier = mobileChatNotifier;
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
        try
        {

    
        var (response, isDoctorToPatient, isStaffToPatient) = await _chatRepository.SendMessageAsync(request);

        try
        {
            await _hub.Clients.Group(request.CrmThreadId).SendAsync("chat.message.created", new
            {
                crmThreadId = request.CrmThreadId,
                crmMessageId = response.CrmMessageId,
                senderType = request.SenderType,
                senderMrNo = request.SenderMrNo,
                senderDoctorLicenseNo = request.SenderDoctorLicenseNo,
                senderEmpId = request.SenderEmpId,
                senderEmpType = request.SenderEmpType,
                receiverType = request.ReceiverType,
                receiverMrNo = request.ReceiverMrNo,
                receiverDoctorLicenseNo = request.ReceiverDoctorLicenseNo,
                receiverStaffType = request.ReceiverStaffType,
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

        // Notify Mobile Backend's ChatHub for real-time delivery
        try
        {
            var idStr = request.CrmThreadId.StartsWith("CRM-TH-", StringComparison.OrdinalIgnoreCase)
                ? request.CrmThreadId.Substring("CRM-TH-".Length) : request.CrmThreadId;
            if (int.TryParse(idStr, out var convId))
            {
                await _mobileChatNotifier.NotifyMessageAsync(
                    convId, 0, request.SenderType, null, request.MessageType,
                    request.Content, request.FileUrl, request.FileName, request.FileSize,
                    request.SentAt == default ? DateTime.UtcNow : request.SentAt,
                    response.CrmMessageId, request.CrmThreadId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify Mobile Backend ChatHub");
        }

        return Ok(response);
        }
        catch (Exception ex)
        {

            throw ex;
        }
    }

    [AllowAnonymous]
    [HttpGet("messages/updates")]
    [ProducesResponseType(typeof(List<ChatMessageUpdateResponse>), 200)]
    public async Task<IActionResult> GetUpdates([FromQuery] DateTime since, [FromQuery] int limit = 100)
    {
        var updates = await _chatRepository.GetDoctorToPatientUpdatesAsync(since.ToUniversalTime(), limit);
        return Ok(updates);
    }

    [AllowAnonymous]
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

    #region Broadcast Channel Endpoints (Staff: Nurse/Receptionist/IVFLab)

    [AllowAnonymous]
    [HttpPost("broadcast-channels/get-or-create")]
    [ProducesResponseType(typeof(ChatBroadcastChannelGetOrCreateResponse), 200)]
    public async Task<IActionResult> GetOrCreateBroadcastChannel([FromBody] ChatBroadcastChannelGetOrCreateRequest request)
    {
        var result = await _chatRepository.GetOrCreateBroadcastChannelAsync(request);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("broadcast-channels")]
    [ProducesResponseType(typeof(List<ChatBroadcastChannelListItemDto>), 200)]
    public async Task<IActionResult> GetBroadcastChannels([FromQuery] string staffType, [FromQuery] int limit = 50)
    {
        var result = await _chatRepository.GetBroadcastChannelsForStaffAsync(staffType, limit);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("broadcast-channels/unread-summary")]
    [ProducesResponseType(typeof(ChatStaffUnreadSummaryResponse), 200)]
    public async Task<IActionResult> GetStaffUnreadSummary([FromQuery] string staffType, [FromQuery] int limit = 50)
    {
        var result = await _chatRepository.GetStaffUnreadSummaryAsync(staffType, limit);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("threads/{crmThreadId}/messages")]
    [ProducesResponseType(typeof(List<ChatThreadMessageDto>), 200)]
    public async Task<IActionResult> GetThreadMessages([FromRoute] string crmThreadId, [FromQuery] int take = 50)
    {
        var messages = await _chatRepository.GetThreadMessagesAsync(crmThreadId, take);
        return Ok(messages);
    }

    [AllowAnonymous]
    [HttpPost("broadcast-channels/{crmThreadId}/mark-read")]
    [ProducesResponseType(typeof(ChatMarkReadResponse), 200)]
    public async Task<IActionResult> MarkBroadcastChannelRead(
        [FromRoute] string crmThreadId,
        [FromQuery] long empId,
        [FromQuery] string staffType)
    {
        var result = await _chatRepository.MarkThreadAsReadByStaffAsync(crmThreadId, empId, staffType);
        return Ok(result);
    }

    #endregion
}
