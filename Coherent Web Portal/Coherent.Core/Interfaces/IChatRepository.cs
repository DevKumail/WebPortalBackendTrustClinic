using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

public interface IChatRepository
{
    Task<ChatThreadGetOrCreateResponse> GetOrCreateThreadAsync(ChatThreadGetOrCreateRequest request);
    Task<ChatBroadcastChannelGetOrCreateResponse> GetOrCreateBroadcastChannelAsync(ChatBroadcastChannelGetOrCreateRequest request);
    Task<(ChatSendMessageResponse Response, bool IsDoctorToPatient, bool IsStaffToPatient)> SendMessageAsync(ChatSendMessageRequest request);
    Task<List<ChatMessageUpdateResponse>> GetDoctorToPatientUpdatesAsync(DateTime sinceUtc, int limit = 100);

    Task<ChatConversationListResponse> GetConversationListAsync(string? doctorLicenseNo, string? patientMrNo, int limit = 50);
    Task<List<ChatBroadcastChannelListItemDto>> GetBroadcastChannelsForStaffAsync(string staffType, int limit = 50);

    Task<ChatDoctorUnreadSummaryResponse> GetDoctorUnreadSummaryAsync(string doctorLicenseNo, int limit = 50);
    Task<ChatStaffUnreadSummaryResponse> GetStaffUnreadSummaryAsync(string staffType, int limit = 50);
    Task<List<ChatThreadMessageDto>> GetThreadMessagesAsync(string crmThreadId, int take = 50);
    Task<ChatMarkReadResponse> MarkThreadAsReadAsync(string crmThreadId, string doctorLicenseNo);
    Task<ChatMarkReadResponse> MarkThreadAsReadByStaffAsync(string crmThreadId, long empId, string staffType);
}
