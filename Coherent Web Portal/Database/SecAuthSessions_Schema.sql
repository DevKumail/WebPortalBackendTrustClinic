USE [UEMedical_For_R&D]
GO

IF OBJECT_ID('[dbo].[SecAuthSession]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SecAuthSession]
    (
        [SessionId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_SecAuthSession_SessionId] DEFAULT (NEWID()) PRIMARY KEY,
        [EmpId] BIGINT NULL,
        [Username] NVARCHAR(150) NULL,
        [RegCode] NVARCHAR(100) NULL,
        [TokenHash] NVARCHAR(128) NOT NULL,
        [TokenLast8] NVARCHAR(8) NULL,
        [IssuedAt] DATETIME2 NOT NULL,
        [ExpiresAt] DATETIME2 NOT NULL,
        [IsLoggedOut] BIT NOT NULL CONSTRAINT [DF_SecAuthSession_IsLoggedOut] DEFAULT (0),
        [LoggedOutAt] DATETIME2 NULL,
        [IpAddress] NVARCHAR(50) NULL,
        [UserAgent] NVARCHAR(512) NULL,
        [RolesCsv] NVARCHAR(MAX) NULL,
        [PermissionsCsv] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_SecAuthSession_CreatedAt] DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX [IX_SecAuthSession_EmpId] ON [dbo].[SecAuthSession]([EmpId]);
    CREATE INDEX [IX_SecAuthSession_Username] ON [dbo].[SecAuthSession]([Username]);
    CREATE INDEX [IX_SecAuthSession_ExpiresAt] ON [dbo].[SecAuthSession]([ExpiresAt]);
    CREATE INDEX [IX_SecAuthSession_TokenHash] ON [dbo].[SecAuthSession]([TokenHash]);

    IF OBJECT_ID('[dbo].[HREmployee]', 'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[SecAuthSession]
        WITH CHECK ADD CONSTRAINT [FK_SecAuthSession_HREmployee] FOREIGN KEY ([EmpId]) REFERENCES [dbo].[HREmployee]([EmpId]);
    END
END
GO
