USE [UEMedical_For_R&D]
GO

IF OBJECT_ID('[dbo].[SecAuthSession]', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SecAuthSession', 'IsLoggedOut') IS NULL
    BEGIN
        ALTER TABLE dbo.SecAuthSession
        ADD IsLoggedOut BIT NOT NULL CONSTRAINT DF_SecAuthSession_IsLoggedOut DEFAULT (0);
    END

    IF COL_LENGTH('dbo.SecAuthSession', 'LoggedOutAt') IS NULL
    BEGIN
        ALTER TABLE dbo.SecAuthSession
        ADD LoggedOutAt DATETIME2 NULL;
    END
END
GO
