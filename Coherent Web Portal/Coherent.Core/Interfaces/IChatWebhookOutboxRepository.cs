namespace Coherent.Core.Interfaces;

public interface IChatWebhookOutboxRepository
{
    Task EnqueueIfNotExistsAsync(string crmMessageId, string crmThreadId, string doctorLicenseNo, string patientMrNo, string payloadJson);
    Task<List<(Guid Id, string CrmMessageId, string PayloadJson, int AttemptCount)>> DequeueDueAsync(int limit = 20);
    Task MarkSucceededAsync(Guid id);
    Task MarkFailedAsync(Guid id, string errorMessage, DateTime nextAttemptAtUtc, int attemptCount);
}
