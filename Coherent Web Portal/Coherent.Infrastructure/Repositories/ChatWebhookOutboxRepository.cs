using Coherent.Core.Interfaces;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class ChatWebhookOutboxRepository : IChatWebhookOutboxRepository
{
    private readonly IDbConnection _connection;

    public ChatWebhookOutboxRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task EnqueueIfNotExistsAsync(string crmMessageId, string crmThreadId, string doctorLicenseNo, string patientMrNo, string payloadJson)
    {
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM dbo.CrmChatWebhookOutbox WHERE CrmMessageId = @CrmMessageId)
            BEGIN
                INSERT INTO dbo.CrmChatWebhookOutbox
                    (EventType, CrmMessageId, CrmThreadId, DoctorLicenseNo, PatientMrNo, PayloadJson, Status)
                VALUES
                    ('DoctorMessageCreated', @CrmMessageId, @CrmThreadId, @DoctorLicenseNo, @PatientMrNo, @PayloadJson, 'Pending')
            END";

        await _connection.ExecuteAsync(sql, new
        {
            CrmMessageId = crmMessageId,
            CrmThreadId = crmThreadId,
            DoctorLicenseNo = doctorLicenseNo,
            PatientMrNo = patientMrNo,
            PayloadJson = payloadJson
        });
    }

    public async Task<List<(Guid Id, string CrmMessageId, string PayloadJson, int AttemptCount)>> DequeueDueAsync(int limit = 20)
    {
        if (limit <= 0)
            limit = 20;

        var sql = @"
            SELECT TOP (@Limit)
                Id,
                CrmMessageId,
                PayloadJson,
                AttemptCount
            FROM dbo.CrmChatWebhookOutbox
            WHERE Status IN ('Pending','Retry')
              AND NextAttemptAt <= SYSUTCDATETIME()
            ORDER BY NextAttemptAt ASC";

        var rows = await _connection.QueryAsync<(Guid Id, string CrmMessageId, string PayloadJson, int AttemptCount)>(sql, new { Limit = limit });
        return rows.ToList();
    }

    public async Task MarkSucceededAsync(Guid id)
    {
        await _connection.ExecuteAsync(
            "UPDATE dbo.CrmChatWebhookOutbox SET Status = 'Succeeded', UpdatedAt = SYSUTCDATETIME(), LastError = NULL WHERE Id = @Id",
            new { Id = id });
    }

    public async Task MarkFailedAsync(Guid id, string errorMessage, DateTime nextAttemptAtUtc, int attemptCount)
    {
        await _connection.ExecuteAsync(
            "UPDATE dbo.CrmChatWebhookOutbox SET Status = 'Retry', AttemptCount = @AttemptCount, NextAttemptAt = @NextAttemptAt, LastError = @LastError, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
            new { Id = id, AttemptCount = attemptCount, NextAttemptAt = nextAttemptAtUtc, LastError = errorMessage });
    }
}
