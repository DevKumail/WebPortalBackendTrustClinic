USE [CoherentMobApp]
GO

IF COL_LENGTH('dbo.MChatMessages', 'ClientMessageId') IS NULL
BEGIN
    ALTER TABLE dbo.MChatMessages
    ADD ClientMessageId UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_MChatMessages_Conversation_ClientMessageId'
      AND object_id = OBJECT_ID('dbo.MChatMessages')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_MChatMessages_Conversation_ClientMessageId
    ON dbo.MChatMessages (ConversationId, ClientMessageId)
    WHERE ClientMessageId IS NOT NULL;
END
GO
