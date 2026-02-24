using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Dapper;
using System.Data;
using System.Linq;

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

    public async Task<(ChatSendMessageResponse Response, bool IsDoctorToPatient, bool IsStaffToPatient)> SendMessageAsync(ChatSendMessageRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CrmThreadId))
                throw new ArgumentException("crmThreadId is required", nameof(request));

            if (request.ClientMessageId == Guid.Empty)
                throw new ArgumentException("clientMessageId is required", nameof(request));

            if (!TryParseConversationId(request.CrmThreadId, out var conversationId))
                throw new ArgumentException("Invalid crmThreadId", nameof(request));

            // Validate messageType - must be one of: text, image, file, audio
            var validMessageTypes = new[] { "text", "image", "file", "audio" };
            var messageType = string.IsNullOrWhiteSpace(request.MessageType) ? "text" : request.MessageType.ToLower();
            if (!validMessageTypes.Contains(messageType))
                throw new ArgumentException($"Invalid messageType '{request.MessageType}'. Must be one of: text, image, file, audio", nameof(request));

            var senderType = NormalizeUserType(request.SenderType);
            var receiverType = NormalizeUserType(request.ReceiverType);

            var senderId = await ResolveUserIdAsync(senderType, request.SenderMrNo, request.SenderDoctorLicenseNo, request.SenderEmpId);

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
                    IsDoctorToPatient: senderType == "Doctor" && receiverType == "Patient",
                    IsStaffToPatient: senderType == "Staff" && receiverType == "Patient"
                );
            }

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
                IsDoctorToPatient: senderType == "Doctor" && receiverType == "Patient",
                IsStaffToPatient: senderType == "Staff" && receiverType == "Patient"
            );
        }
        catch (Exception ex)
        {
            throw;
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

    public async Task<ChatConversationListResponse> GetConversationListAsync(string? doctorLicenseNo, string? patientMrNo, int limit = 50)
    {
        if (limit <= 0)
            limit = 50;

        limit = Math.Min(limit, 200);

        var hasDoctor = !string.IsNullOrWhiteSpace(doctorLicenseNo);
        var hasPatient = !string.IsNullOrWhiteSpace(patientMrNo);

        if (hasDoctor == hasPatient)
            throw new ArgumentException("Either doctorLicenseNo or patientMrNo must be provided (but not both)");

        if (hasDoctor)
        {
            var sql = @"
SELECT TOP (@Limit)
    c.ConversationId,
    c.LastMessageAt,
    c.LastMessage AS LastMessagePreview,
    pPatient.UserId AS PatientUserId,
    u.MRNO AS PatientMrNo,
    COALESCE(NULLIF(u.FullName, ''), NULLIF(u.MRNO, ''), CAST(u.Id AS NVARCHAR(50))) AS PatientName,
    (
        SELECT COUNT(1)
        FROM dbo.MChatMessages m
        WHERE m.ConversationId = c.ConversationId
          AND m.SenderType = 'Patient'
          AND m.IsRead = 0
          AND m.IsDeleted = 0
    ) AS UnreadCount
FROM dbo.MConversations c
INNER JOIN dbo.MConversationParticipants pDoctor
    ON pDoctor.ConversationId = c.ConversationId
   AND pDoctor.UserType = 'Doctor'
INNER JOIN dbo.MDoctors d
    ON d.DId = pDoctor.UserId
INNER JOIN dbo.MConversationParticipants pPatient
    ON pPatient.ConversationId = c.ConversationId
   AND pPatient.UserType = 'Patient'
INNER JOIN dbo.Users u
    ON u.Id = pPatient.UserId
WHERE d.LicenceNo = @DoctorLicenseNo
ORDER BY COALESCE(c.LastMessageAt, '1900-01-01') DESC, c.ConversationId DESC;";

            var rows = await _connection.QueryAsync<dynamic>(sql, new { DoctorLicenseNo = doctorLicenseNo, Limit = limit });

            var response = new ChatConversationListResponse
            {
                DoctorLicenseNo = doctorLicenseNo,
                ServerTimeUtc = DateTime.UtcNow
            };

            foreach (var row in rows)
            {
                var conversationId = (int)row.ConversationId;
                response.Conversations.Add(new ChatConversationListItemDto
                {
                    ConversationId = conversationId,
                    CrmThreadId = $"CRM-TH-{conversationId}",
                    LastMessageAt = row.LastMessageAt,
                    LastMessagePreview = row.LastMessagePreview,
                    UnreadCount = (int)(row.UnreadCount ?? 0),
                    Counterpart = new ChatConversationCounterpartDto
                    {
                        UserType = "Patient",
                        PatientMrNo = row.PatientMrNo,
                        PatientName = row.PatientName
                    }
                });
            }

            return response;
        }

        {
            var sql = @"
SELECT TOP (@Limit)
    c.ConversationId,
    c.LastMessageAt,
    c.LastMessage AS LastMessagePreview,
    d.LicenceNo AS DoctorLicenseNo,
    d.DoctorName,
    d.Title,
    d.DoctorPhotoName,
    (
        SELECT COUNT(1)
        FROM dbo.MChatMessages m
        WHERE m.ConversationId = c.ConversationId
          AND m.SenderType = 'Doctor'
          AND m.IsRead = 0
          AND m.IsDeleted = 0
    ) AS UnreadCount
FROM dbo.MConversations c
INNER JOIN dbo.MConversationParticipants pPatient
    ON pPatient.ConversationId = c.ConversationId
   AND pPatient.UserType = 'Patient'
INNER JOIN dbo.Users u
    ON u.Id = pPatient.UserId
INNER JOIN dbo.MConversationParticipants pDoctor
    ON pDoctor.ConversationId = c.ConversationId
   AND pDoctor.UserType = 'Doctor'
INNER JOIN dbo.MDoctors d
    ON d.DId = pDoctor.UserId
WHERE u.MRNO = @PatientMrNo
ORDER BY COALESCE(c.LastMessageAt, '1900-01-01') DESC, c.ConversationId DESC;";

            var rows = await _connection.QueryAsync<dynamic>(sql, new { PatientMrNo = patientMrNo, Limit = limit });

            var response = new ChatConversationListResponse
            {
                PatientMrNo = patientMrNo,
                ServerTimeUtc = DateTime.UtcNow
            };

            foreach (var row in rows)
            {
                var conversationId = (int)row.ConversationId;
                response.Conversations.Add(new ChatConversationListItemDto
                {
                    ConversationId = conversationId,
                    CrmThreadId = $"CRM-TH-{conversationId}",
                    LastMessageAt = row.LastMessageAt,
                    LastMessagePreview = row.LastMessagePreview,
                    UnreadCount = (int)(row.UnreadCount ?? 0),
                    Counterpart = new ChatConversationCounterpartDto
                    {
                        UserType = "Doctor",
                        DoctorLicenseNo = row.DoctorLicenseNo,
                        DoctorName = row.DoctorName,
                        DoctorTitle = row.Title,
                        DoctorPhotoName = row.DoctorPhotoName
                    }
                });
            }

            return response;
        }
    }

    public async Task<ChatDoctorUnreadSummaryResponse> GetDoctorUnreadSummaryAsync(string doctorLicenseNo, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(doctorLicenseNo))
            throw new ArgumentException("doctorLicenseNo is required", nameof(doctorLicenseNo));

        if (limit <= 0)
            limit = 50;

        var sql = @"
            SELECT TOP (@Limit)
                c.ConversationId,
                u.MRNO AS PatientMrNo,
                rp.FullName AS PatientName,
                SUM(CASE WHEN m.IsRead = 0 THEN 1 ELSE 0 END) AS UnreadCount,
                MAX(m.SentAt) AS LastMessageAt,
                MAX(COALESCE(NULLIF(m.Content, ''), NULLIF(m.FileName, ''), m.MessageType)) AS LastMessagePreview
            FROM dbo.MConversations c
            INNER JOIN dbo.MConversationParticipants pDoctor ON pDoctor.ConversationId = c.ConversationId AND pDoctor.UserType = 'Doctor'
            INNER JOIN dbo.MDoctors d ON d.DId = pDoctor.UserId
            INNER JOIN dbo.MConversationParticipants pPatient ON pPatient.ConversationId = c.ConversationId AND pPatient.UserType = 'Patient'
            INNER JOIN dbo.Users u ON u.Id = pPatient.UserId
            LEFT JOIN dbo.Users rp ON rp.MRNO = u.MRNO
            INNER JOIN dbo.MChatMessages m ON m.ConversationId = c.ConversationId
            WHERE d.LicenceNo = @DoctorLicenseNo
              AND m.SenderType = 'Patient'
              AND m.IsDeleted = 0
            GROUP BY
                c.ConversationId,
                u.MRNO,
                rp.FullName
            HAVING SUM(CASE WHEN m.IsRead = 0 THEN 1 ELSE 0 END) > 0
            ORDER BY MAX(m.SentAt) DESC";

        var rows = await _connection.QueryAsync<dynamic>(sql, new { DoctorLicenseNo = doctorLicenseNo, Limit = limit });

        var response = new ChatDoctorUnreadSummaryResponse
        {
            DoctorLicenseNo = doctorLicenseNo
        };

        foreach (var row in rows)
        {
            var conversationId = (int)row.ConversationId;
            var patientName = (row.PatientName as string) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(patientName))
                patientName = (string)row.PatientMrNo;

            var item = new ChatUnreadThreadItem
            {
                CrmThreadId = $"CRM-TH-{conversationId}",
                PatientMrNo = row.PatientMrNo,
                PatientName = patientName,
                UnreadCount = (int)row.UnreadCount,
                LastMessageAt = row.LastMessageAt,
                LastMessagePreview = row.LastMessagePreview
            };

            response.Threads.Add(item);
            response.TotalUnread += item.UnreadCount;
        }

        return response;
    }

    public async Task<List<ChatThreadMessageDto>> GetThreadMessagesAsync(string crmThreadId, int take = 50)
    {
        if (take <= 0)
            take = 50;

        if (!TryParseConversationId(crmThreadId, out var conversationId))
            throw new ArgumentException("Invalid crmThreadId", nameof(crmThreadId));

        var sql = @"
            SELECT TOP (@Take)
                m.MessageId,
                m.SenderType,
                m.MessageType,
                m.Content,
                m.FileUrl,
                m.FileName,
                m.FileSize,
                m.SentAt
            FROM dbo.MChatMessages m
            WHERE m.ConversationId = @ConversationId
              AND m.IsDeleted = 0
            ORDER BY m.SentAt DESC, m.MessageId DESC";

        var rows = await _connection.QueryAsync<dynamic>(sql, new { ConversationId = conversationId, Take = take });

        // Return ascending for UI rendering
        return rows
            .Select(r => new ChatThreadMessageDto
            {
                CrmMessageId = $"CRM-MSG-{(int)r.MessageId}",
                CrmThreadId = $"CRM-TH-{conversationId}",
                SenderType = r.SenderType,
                MessageType = r.MessageType,
                Content = r.Content,
                FileUrl = r.FileUrl,
                FileName = r.FileName,
                FileSize = r.FileSize,
                SentAt = r.SentAt
            })
            .OrderBy(x => x.SentAt)
            .ThenBy(x => int.Parse(x.CrmMessageId.Substring("CRM-MSG-".Length)))
            .ToList();
    }

    public async Task<ChatMarkReadResponse> MarkThreadAsReadAsync(string crmThreadId, string doctorLicenseNo)
    {
        if (string.IsNullOrWhiteSpace(doctorLicenseNo))
            throw new ArgumentException("doctorLicenseNo is required", nameof(doctorLicenseNo));

        if (!TryParseConversationId(crmThreadId, out var conversationId))
            throw new ArgumentException("Invalid crmThreadId", nameof(crmThreadId));

        // Ensure this doctor belongs to this conversation
        var isParticipant = await _connection.QueryFirstOrDefaultAsync<int>(@"
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM dbo.MConversationParticipants p
                INNER JOIN dbo.MDoctors d ON d.DId = p.UserId
                WHERE p.ConversationId = @ConversationId
                  AND p.UserType = 'Doctor'
                  AND d.LicenceNo = @DoctorLicenseNo
            ) THEN 1 ELSE 0 END",
            new { ConversationId = conversationId, DoctorLicenseNo = doctorLicenseNo });

        if (isParticipant != 1)
            throw new InvalidOperationException("Doctor is not a participant of this conversation");

        var affected = await _connection.ExecuteAsync(@"
            UPDATE dbo.MChatMessages
            SET IsRead = 1
            WHERE ConversationId = @ConversationId
              AND SenderType = 'Patient'
              AND IsRead = 0
              AND IsDeleted = 0",
            new { ConversationId = conversationId });

        return new ChatMarkReadResponse
        {
            CrmThreadId = $"CRM-TH-{conversationId}",
            MarkedReadCount = affected,
            ServerProcessedAt = DateTime.UtcNow
        };
    }

    public async Task<ChatBroadcastChannelGetOrCreateResponse> GetOrCreateBroadcastChannelAsync(ChatBroadcastChannelGetOrCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PatientMrNo))
            throw new ArgumentException("patientMrNo is required", nameof(request));

        if (string.IsNullOrWhiteSpace(request.StaffType))
            throw new ArgumentException("staffType is required", nameof(request));

        var validStaffTypes = new[] { "Nurse", "Receptionist", "IVFLab" , "OTNurse" };
        if (!validStaffTypes.Contains(request.StaffType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"staffType must be one of: {string.Join(", ", validStaffTypes)}", nameof(request));

        var patientId = await _connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT TOP 1 Id FROM dbo.Users WHERE MRNO = @MRNO AND IsDeleted = 0",
            new { MRNO = request.PatientMrNo });

        if (patientId == null)
            throw new InvalidOperationException($"Patient not found for MRNO {request.PatientMrNo}");

        // Call stored procedure to get or create broadcast channel
        var result = await _connection.QueryFirstOrDefaultAsync<dynamic>(
            "EXEC dbo.SP_CreateOrGetBroadcastChannel @PatientUserId, @StaffType",
            new { PatientUserId = patientId.Value, StaffType = request.StaffType });

        if (result == null || result.ConversationId <= 0)
            throw new InvalidOperationException("Failed to create or get broadcast channel");

        return new ChatBroadcastChannelGetOrCreateResponse
        {
            CrmThreadId = $"CRM-TH-{result.ConversationId}",
            ChannelType = "Broadcast",
            PatientMrNo = request.PatientMrNo,
            StaffType = request.StaffType,
            ChannelTitle = result.ChannelTitle
        };
    }

    public async Task<List<ChatBroadcastChannelListItemDto>> GetBroadcastChannelsForStaffAsync(string staffType, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(staffType))
            throw new ArgumentException("staffType is required", nameof(staffType));

        if (limit <= 0)
            limit = 50;

        limit = Math.Min(limit, 200);

        var channelTitle = $"{staffType} Support Channel";

        var sql = @"
            SELECT TOP (@Limit)
                c.ConversationId,
                c.Title AS ChannelTitle,
                c.LastMessageAt,
                c.LastMessage AS LastMessagePreview,
                u.MRNO AS PatientMrNo,
                COALESCE(NULLIF(u.FullName, ''), NULLIF(u.MRNO, ''), CAST(u.Id AS NVARCHAR(50))) AS PatientName,
                (
                    SELECT COUNT(1)
                    FROM dbo.MChatMessages m
                    WHERE m.ConversationId = c.ConversationId
                      AND m.SenderType = 'Patient'
                      AND m.IsRead = 0
                      AND m.IsDeleted = 0
                ) AS UnreadCount
            FROM dbo.MConversations c
            INNER JOIN dbo.MConversationParticipants cp
                ON cp.ConversationId = c.ConversationId
               AND cp.UserType = 'Patient'
            INNER JOIN dbo.Users u
                ON u.Id = cp.UserId
            WHERE c.ConversationType = 'Support'
              AND c.Title = @ChannelTitle
              AND c.IsActive = 1
            ORDER BY COALESCE(c.LastMessageAt, '1900-01-01') DESC, c.ConversationId DESC";

        var rows = await _connection.QueryAsync<dynamic>(sql, new { ChannelTitle = channelTitle, Limit = limit });

        return rows.Select(row => new ChatBroadcastChannelListItemDto
        {
            ConversationId = (int)row.ConversationId,
            CrmThreadId = $"CRM-TH-{row.ConversationId}",
            ChannelType = "Broadcast",
            StaffType = staffType,
            PatientMrNo = row.PatientMrNo,
            PatientName = row.PatientName,
            LastMessageAt = row.LastMessageAt,
            LastMessagePreview = row.LastMessagePreview,
            UnreadCount = (int)(row.UnreadCount ?? 0)
        }).ToList();
    }

    public async Task<ChatStaffUnreadSummaryResponse> GetStaffUnreadSummaryAsync(string staffType, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(staffType))
            throw new ArgumentException("staffType is required", nameof(staffType));

        if (limit <= 0)
            limit = 50;

        var channelTitle = $"{staffType} Support Channel";

        var sql = @"
            SELECT TOP (@Limit)
                c.ConversationId,
                u.MRNO AS PatientMrNo,
                COALESCE(NULLIF(u.FullName, ''), NULLIF(u.MRNO, ''), CAST(u.Id AS NVARCHAR(50))) AS PatientName,
                c.LastMessageAt,
                c.LastMessage AS LastMessagePreview,
                (
                    SELECT COUNT(1)
                    FROM dbo.MChatMessages m
                    WHERE m.ConversationId = c.ConversationId
                      AND m.SenderType = 'Patient'
                      AND m.IsRead = 0
                      AND m.IsDeleted = 0
                ) AS UnreadCount
            FROM dbo.MConversations c
            INNER JOIN dbo.MConversationParticipants cp
                ON cp.ConversationId = c.ConversationId
               AND cp.UserType = 'Patient'
            INNER JOIN dbo.Users u
                ON u.Id = cp.UserId
            WHERE c.ConversationType = 'Support'
              AND c.Title = @ChannelTitle
              AND c.IsActive = 1
              AND EXISTS (
                  SELECT 1 FROM dbo.MChatMessages m2
                  WHERE m2.ConversationId = c.ConversationId
                    AND m2.SenderType = 'Patient'
                    AND m2.IsRead = 0
                    AND m2.IsDeleted = 0
              )
            ORDER BY c.LastMessageAt DESC";

        var rows = await _connection.QueryAsync<dynamic>(sql, new { ChannelTitle = channelTitle, Limit = limit });

        var response = new ChatStaffUnreadSummaryResponse
        {
            StaffType = staffType
        };

        foreach (var row in rows)
        {
            var item = new ChatBroadcastChannelListItemDto
            {
                ConversationId = (int)row.ConversationId,
                CrmThreadId = $"CRM-TH-{row.ConversationId}",
                ChannelType = "Broadcast",
                StaffType = staffType,
                PatientMrNo = row.PatientMrNo,
                PatientName = row.PatientName,
                LastMessageAt = row.LastMessageAt,
                LastMessagePreview = row.LastMessagePreview,
                UnreadCount = (int)(row.UnreadCount ?? 0)
            };

            response.Channels.Add(item);
            response.TotalUnread += item.UnreadCount;
        }

        return response;
    }

    public async Task<ChatMarkReadResponse> MarkThreadAsReadByStaffAsync(string crmThreadId, long empId, string staffType)
    {
        if (string.IsNullOrWhiteSpace(staffType))
            throw new ArgumentException("staffType is required", nameof(staffType));

        if (!TryParseConversationId(crmThreadId, out var conversationId))
            throw new ArgumentException("Invalid crmThreadId", nameof(crmThreadId));

        var channelTitle = $"{staffType} Support Channel";

        // Verify this is a valid broadcast channel for the staff type
        var isValidChannel = await _connection.QueryFirstOrDefaultAsync<int>(@"
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM dbo.MConversations c
                WHERE c.ConversationId = @ConversationId
                  AND c.ConversationType = 'Support'
                  AND c.Title = @ChannelTitle
                  AND c.IsActive = 1
            ) THEN 1 ELSE 0 END",
            new { ConversationId = conversationId, ChannelTitle = channelTitle });

        if (isValidChannel != 1)
            throw new InvalidOperationException("This is not a valid broadcast channel for the specified staff type");

        var affected = await _connection.ExecuteAsync(@"
            UPDATE dbo.MChatMessages
            SET IsRead = 1
            WHERE ConversationId = @ConversationId
              AND SenderType = 'Patient'
              AND IsRead = 0
              AND IsDeleted = 0",
            new { ConversationId = conversationId });

        return new ChatMarkReadResponse
        {
            CrmThreadId = $"CRM-TH-{conversationId}",
            MarkedReadCount = affected,
            ServerProcessedAt = DateTime.UtcNow
        };
    }

    private async Task<int> ResolveUserIdAsync(string userType, string? mrNo, string? licenceNo, long? empId = null)
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

        if (userType == "Staff")
        {
            if (!empId.HasValue)
                throw new ArgumentException("senderEmpId is required for Staff");

            // For Staff, we use EmpId directly as SenderId
            // Note: Staff members are identified by their HREmployee.EmpId
            return (int)empId.Value;
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
