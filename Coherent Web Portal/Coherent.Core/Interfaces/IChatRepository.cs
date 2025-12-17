using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

public interface IChatRepository
{
    Task<ChatThreadGetOrCreateResponse> GetOrCreateThreadAsync(ChatThreadGetOrCreateRequest request);
    Task<(ChatSendMessageResponse Response, bool IsDoctorToPatient)> SendMessageAsync(ChatSendMessageRequest request);
    Task<List<ChatMessageUpdateResponse>> GetDoctorToPatientUpdatesAsync(DateTime sinceUtc, int limit = 100);
}
