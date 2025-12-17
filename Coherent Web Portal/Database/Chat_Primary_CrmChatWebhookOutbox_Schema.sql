USE [UEMedical_For_R&D]
GO

IF OBJECT_ID('[dbo].[CrmChatWebhookOutbox]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CrmChatWebhookOutbox]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_CrmChatWebhookOutbox] PRIMARY KEY DEFAULT NEWID(),
        [EventType] NVARCHAR(100) NOT NULL,
        [CrmMessageId] NVARCHAR(50) NOT NULL,
        [CrmThreadId] NVARCHAR(50) NOT NULL,
        [DoctorLicenseNo] NVARCHAR(20) NOT NULL,
        [PatientMrNo] NVARCHAR(50) NOT NULL,
        [PayloadJson] NVARCHAR(MAX) NOT NULL,
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_CrmChatWebhookOutbox_Status] DEFAULT ('Pending'),
        [AttemptCount] INT NOT NULL CONSTRAINT [DF_CrmChatWebhookOutbox_AttemptCount] DEFAULT (0),
        [NextAttemptAt] DATETIME2 NOT NULL CONSTRAINT [DF_CrmChatWebhookOutbox_NextAttemptAt] DEFAULT (SYSUTCDATETIME()),
        [LastError] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_CrmChatWebhookOutbox_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT [UX_CrmChatWebhookOutbox_CrmMessageId] UNIQUE ([CrmMessageId])
    );

    CREATE INDEX [IX_CrmChatWebhookOutbox_Status_NextAttemptAt]
    ON [dbo].[CrmChatWebhookOutbox] ([Status], [NextAttemptAt]);
END
GO
