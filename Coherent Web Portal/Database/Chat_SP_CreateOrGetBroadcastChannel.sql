USE [CoherentMobApp]
GO

IF OBJECT_ID('[dbo].[SP_CreateOrGetBroadcastChannel]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[SP_CreateOrGetBroadcastChannel];
GO

-- =============================================
-- SP_CreateOrGetBroadcastChannel
-- Creates or retrieves a broadcast channel for Patient ↔ Staff Type communication
-- Each patient has ONE channel per staff type (Nurse, Receptionist, IVFLab)
-- Messages in this channel are visible to ALL staff of that type
-- =============================================
CREATE PROCEDURE [dbo].[SP_CreateOrGetBroadcastChannel]
    @PatientUserId INT,
    @StaffType NVARCHAR(50)  -- e.g., 'Nurse', 'Receptionist', 'IVFLab'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ConversationId INT
    DECLARE @ChannelTitle NVARCHAR(200)
    
    -- Build channel title
    SET @ChannelTitle = @StaffType + ' Support Channel'

    -- Check if broadcast channel already exists for this patient + staff type
    -- We use ConversationType = 'Support' and Title contains the StaffType
    SELECT TOP 1 @ConversationId = c.[ConversationId]
    FROM [MConversations] c
    INNER JOIN [MConversationParticipants] cp 
        ON c.[ConversationId] = cp.[ConversationId]
    WHERE cp.[UserId] = @PatientUserId 
        AND cp.[UserType] = 'Patient'
        AND c.[ConversationType] = 'Support'
        AND c.[Title] = @ChannelTitle
        AND c.[IsActive] = 1

    IF @ConversationId IS NULL
    BEGIN
        -- Create new broadcast channel
        INSERT INTO [MConversations] ([ConversationType], [Title], [CreatedBy])
        VALUES ('Support', @ChannelTitle, @PatientUserId)
        
        SET @ConversationId = SCOPE_IDENTITY()

        -- Add patient as participant
        INSERT INTO [MConversationParticipants] ([ConversationId], [UserId], [UserType])
        VALUES (@ConversationId, @PatientUserId, 'Patient')
        
        -- Note: Staff members are NOT added as individual participants
        -- Instead, all staff of the specified type can see/respond to this channel
        -- This is handled in application logic based on StaffType
    END

    SELECT 
        @ConversationId AS ConversationId,
        @ChannelTitle AS ChannelTitle
END
GO

PRINT 'SP_CreateOrGetBroadcastChannel created successfully';
GO
