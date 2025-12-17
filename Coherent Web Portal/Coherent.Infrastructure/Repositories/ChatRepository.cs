using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly IDbConnection _connection;

    public ChatRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<ChatThreadGetOrCreateResponse> GetOrCreateThreadAsync(ChatThreadGetOrCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PatientMrNo))
            throw new ArgumentException("patientMrNo is required", nameof(request));

        if (string.IsNullOrWhiteSpace(request.DoctorLicenseNo))
            throw new ArgumentException("doctorLicenseNo is required", nameof(request));

        var patientId = await _connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT TOP 1 Id FROM dbo.Users WHERE MRNO = @MRNO AND IsDeleted = 0",
            new { MRNO = request.PatientMrNo });

        if (patientId == null)
            throw new InvalidOperationException($"Patient not found for MRNO {request.PatientMrNo}");

        var doctorId = await _connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT TOP 1 DId FROM dbo.MDoctors WHERE LicenceNo = @LicenceNo",
            new { LicenceNo = request.DoctorLicenseNo });

        if (doctorId == null)
            throw new InvalidOperationException($"Doctor not found for LicenceNo {request.DoctorLicenseNo}");

        // Ensure thread exists (idempotent)
        var conversationId = await _connection.QueryFirstOrDefaultAsync<int?>(
            "EXEC dbo.SP_CreateOrGetConversation @User1Id, @User1Type, @User2Id, @User2Type",
            new { User1Id = patientId.Value, User1Type = "Patient", User2Id = doctorId.Value, User2Type = "Doctor" });

        if (conversationId == null || conversationId.Value <= 0)
            throw new InvalidOperationException("Failed to create or get conversation");

        return new ChatThreadGetOrCreateResponse
        {
            CrmThreadId = $"CRM-TH-{conversationId.Value}",
            PatientMrNo = request.PatientMrNo,
            DoctorLicenseNo = request.DoctorLicenseNo
        };
    }

    public async Task<(ChatSendMessageResponse Response, bool IsDoctorToPatient)> SendMessageAsync(ChatSendMessageRequest request)
    {
        try
        {

        
        if (string.IsNullOrWhiteSpace(request.CrmThreadId))
            throw new ArgumentException("crmThreadId is required", nameof(request));

        if (request.ClientMessageId == Guid.Empty)
            throw new ArgumentException("clientMessageId is required", nameof(request));

        if (!TryParseConversationId(request.CrmThreadId, out var conversationId))
            throw new ArgumentException("Invalid crmThreadId", nameof(request));

        var senderType = NormalizeUserType(request.SenderType);
        var receiverType = NormalizeUserType(request.ReceiverType);

        var senderId = await ResolveUserIdAsync(senderType, request.SenderMrNo, request.SenderDoctorLicenseNo);
        var receiverId = await ResolveUserIdAsync(receiverType, request.ReceiverMrNo, request.ReceiverDoctorLicenseNo);

        // Idempotency check
        var existingMessageId = await _connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT TOP 1 MessageId FROM dbo.MChatMessages WHERE ConversationId = @ConversationId AND ClientMessageId = @ClientMessageId",
            new { ConversationId = conversationId, ClientMessageId = request.ClientMessageId });

        if (existingMessageId.HasValue)
        {
            return (
                new ChatSendMessageResponse
                {
                    CrmMessageId = $"CRM-MSG-{existingMessageId.Value}",
                    CrmThreadId = request.CrmThreadId,
                    Status = "Accepted",
                    ServerReceivedAt = DateTime.UtcNow
                },
                IsDoctorToPatient: senderType == "Doctor" && receiverType == "Patient"
            );
        }

        var messageType = string.IsNullOrWhiteSpace(request.MessageType) ? "Text" : request.MessageType;

        // Insert message
        var insertSql = @"
            INSERT INTO dbo.MChatMessages
                (ConversationId, SenderId, SenderType, MessageType, Content, FileUrl, FileName, FileSize, SentAt, IsDelivered, IsRead, IsDeleted, ClientMessageId)
            VALUES
                (@ConversationId, @SenderId, @SenderType, @MessageType, @Content, @FileUrl, @FileName, @FileSize, @SentAt, 0, 0, 0, @ClientMessageId);

            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var messageId = await _connection.QuerySingleAsync<int>(insertSql, new
        {
            ConversationId = conversationId,
            SenderId = senderId,
            SenderType = senderType,
            MessageType = messageType,
            Content = request.Content,
            FileUrl = request.FileUrl,
            FileName = request.FileName,
            FileSize = request.FileSize,
            SentAt = request.SentAt == default ? DateTime.UtcNow : request.SentAt,
            ClientMessageId = request.ClientMessageId
        });

        // Update conversation last message
        await _connection.ExecuteAsync(
            "UPDATE dbo.MConversations SET LastMessageAt = @Now, LastMessage = @LastMessage WHERE ConversationId = @ConversationId",
            new
            {
                Now = DateTime.UtcNow,
                LastMessage = BuildLastMessage(messageType, request.Content, request.FileName),
                ConversationId = conversationId
            });

        return (
            new ChatSendMessageResponse
            {
                CrmMessageId = $"CRM-MSG-{messageId}",
                CrmThreadId = request.CrmThreadId,
                Status = "Accepted",
                ServerReceivedAt = DateTime.UtcNow
            },
            IsDoctorToPatient: senderType == "Doctor" && receiverType == "Patient"
        );

        }
        catch (Exception ex)
        {

            throw ex;
        }
    }

    public async Task<List<ChatMessageUpdateResponse>> GetDoctorToPatientUpdatesAsync(DateTime sinceUtc, int limit = 100)
    {
        if (limit <= 0)
            limit = 100;

        var sql = @"
            SELECT TOP (@Limit)
                c.ConversationId,
                m.MessageId,
                m.SentAt,
                d.LicenceNo AS DoctorLicenseNo,
                u.MRNO AS PatientMrNo,
                m.MessageType,
                m.Content,
                m.FileUrl,
                m.FileName,
                m.FileSize
            FROM dbo.MChatMessages m
            INNER JOIN dbo.MConversations c ON c.ConversationId = m.ConversationId
            INNER JOIN dbo.MConversationParticipants pDoctor ON pDoctor.ConversationId = c.ConversationId AND pDoctor.UserType = 'Doctor'
            INNER JOIN dbo.MConversationParticipants pPatient ON pPatient.ConversationId = c.ConversationId AND pPatient.UserType = 'Patient'
            INNER JOIN dbo.MDoctors d ON d.DId = pDoctor.UserId
            INNER JOIN dbo.Users u ON u.Id = pPatient.UserId
            WHERE m.SenderType = 'Doctor'
              AND m.SentAt > @SinceUtc
              AND m.IsDeleted = 0
            ORDER BY m.SentAt ASC";

        var rows = await _connection.QueryAsync<dynamic>(sql, new { SinceUtc = sinceUtc, Limit = limit });

        var result = new List<ChatMessageUpdateResponse>();
        foreach (var row in rows)
        {
            var conversationId = (int)row.ConversationId;
            var messageId = (int)row.MessageId;

            result.Add(new ChatMessageUpdateResponse
            {
                EventType = "DoctorMessageCreated",
                CrmThreadId = $"CRM-TH-{conversationId}",
                CrmMessageId = $"CRM-MSG-{messageId}",
                DoctorLicenseNo = row.DoctorLicenseNo,
                PatientMrNo = row.PatientMrNo,
                MessageType = row.MessageType,
                Content = row.Content,
                FileUrl = row.FileUrl,
                FileName = row.FileName,
                FileSize = row.FileSize,
                SentAt = row.SentAt
            });
        }

        return result;
    }

    private async Task<int> ResolveUserIdAsync(string userType, string? mrNo, string? licenceNo)
    {
        if (userType == "Patient")
        {
            if (string.IsNullOrWhiteSpace(mrNo))
                throw new ArgumentException("senderMrNo/receiverMrNo is required for Patient");

            var patientId = await _connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT TOP 1 Id FROM dbo.Users WHERE MRNO = @MRNO AND IsDeleted = 0",
                new { MRNO = mrNo });

            if (patientId == null)
                throw new InvalidOperationException($"Patient not found for MRNO {mrNo}");

            return patientId.Value;
        }

        if (userType == "Doctor")
        {
            if (string.IsNullOrWhiteSpace(licenceNo))
                throw new ArgumentException("senderDoctorLicenseNo/receiverDoctorLicenseNo is required for Doctor");

            var doctorId = await _connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT TOP 1 DId FROM dbo.MDoctors WHERE LicenceNo = @LicenceNo",
                new { LicenceNo = licenceNo });

            if (doctorId == null)
                throw new InvalidOperationException($"Doctor not found for LicenceNo {licenceNo}");

            return doctorId.Value;
        }

        throw new ArgumentException($"Unsupported user type: {userType}");
    }

    private static string NormalizeUserType(string userType)
    {
        if (string.Equals(userType, "Patient", StringComparison.OrdinalIgnoreCase)) return "Patient";
        if (string.Equals(userType, "Doctor", StringComparison.OrdinalIgnoreCase)) return "Doctor";
        if (string.Equals(userType, "Staff", StringComparison.OrdinalIgnoreCase)) return "Staff";
        throw new ArgumentException($"Invalid userType: {userType}");
    }

    private static bool TryParseConversationId(string crmThreadId, out int conversationId)
    {
        conversationId = 0;
        if (string.IsNullOrWhiteSpace(crmThreadId)) return false;

        if (crmThreadId.StartsWith("CRM-TH-", StringComparison.OrdinalIgnoreCase))
            crmThreadId = crmThreadId.Substring("CRM-TH-".Length);

        return int.TryParse(crmThreadId, out conversationId) && conversationId > 0;
    }

    private static string BuildLastMessage(string messageType, string? content, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(content))
            return content.Length > 500 ? content.Substring(0, 500) : content;

        if (!string.IsNullOrWhiteSpace(fileName))
            return fileName.Length > 500 ? fileName.Substring(0, 500) : fileName;

        return messageType;
    }
}
