-- =============================================
-- Coherent Web Portal - Secondary Database Schema
-- Third-Party Integration & Request Logging
-- Database: CoherentMobApp
-- ADHICS Compliant
-- =============================================

USE [CoherentMobApp]
GO

-- =============================================
-- ThirdPartyClients Table
-- =============================================
CREATE TABLE [dbo].[ThirdPartyClients] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ClientName] NVARCHAR(200) NOT NULL,
    [ClientId] NVARCHAR(100) NOT NULL UNIQUE,
    [ApiKeyHash] NVARCHAR(500) NOT NULL,
    [IpWhitelist] NVARCHAR(MAX) NOT NULL, -- Comma-separated IPs
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    [LastAccessAt] DATETIME2 NULL,
    [ComplianceLevel] NVARCHAR(50) NOT NULL DEFAULT 'ADHICS-Level1',
    [SecurityKeyHash] NVARCHAR(500) NOT NULL,
    [SecurityKeyExpiry] DATETIME2 NOT NULL,
    [MaxRequestsPerMinute] INT NOT NULL DEFAULT 60,
    [AllowedEndpoints] NVARCHAR(MAX) NOT NULL, -- JSON array
    [DataAccessLevel] NVARCHAR(20) NOT NULL, -- Read, Write, Full
    INDEX IX_ThirdPartyClients_ClientId NONCLUSTERED ([ClientId]),
    INDEX IX_ThirdPartyClients_IsActive NONCLUSTERED ([IsActive])
);
GO

-- =============================================
-- ThirdPartyRequestLogs Table (ADHICS Compliance)
-- =============================================
CREATE TABLE [dbo].[ThirdPartyRequestLogs] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [ThirdPartyClientId] UNIQUEIDENTIFIER NOT NULL,
    [ClientName] NVARCHAR(200) NOT NULL,
    [Endpoint] NVARCHAR(500) NOT NULL,
    [HttpMethod] NVARCHAR(10) NOT NULL,
    [RequestPayload] NVARCHAR(MAX) NOT NULL,
    [ResponsePayload] NVARCHAR(MAX) NULL,
    [StatusCode] INT NOT NULL,
    [IpAddress] NVARCHAR(50) NOT NULL,
    [RequestTimestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ResponseTimestamp] DATETIME2 NULL,
    [DurationMs] BIGINT NOT NULL,
    [IsSuccess] BIT NOT NULL DEFAULT 1,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [SecurityValidationResult] NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_ThirdPartyRequestLogs_Clients FOREIGN KEY ([ThirdPartyClientId]) 
        REFERENCES [dbo].[ThirdPartyClients]([Id]) ON DELETE CASCADE,
    INDEX IX_ThirdPartyRequestLogs_ClientId NONCLUSTERED ([ThirdPartyClientId]),
    INDEX IX_ThirdPartyRequestLogs_Timestamp NONCLUSTERED ([RequestTimestamp] DESC),
    INDEX IX_ThirdPartyRequestLogs_IsSuccess NONCLUSTERED ([IsSuccess]),
    INDEX IX_ThirdPartyRequestLogs_StatusCode NONCLUSTERED ([StatusCode])
);
GO

-- =============================================
-- Add Comments for ADHICS Compliance
-- =============================================
EXEC sp_addextendedproperty 
    @name = N'ADHICS_Compliance', 
    @value = N'Third-party client registration with security key validation and IP whitelisting', 
    @level0type = N'SCHEMA', @level0name = 'dbo', 
    @level1type = N'TABLE', @level1name = 'ThirdPartyClients';
GO

EXEC sp_addextendedproperty 
    @name = N'ADHICS_Compliance', 
    @value = N'Comprehensive logging of all third-party API requests for audit and compliance', 
    @level0type = N'SCHEMA', @level0name = 'dbo', 
    @level1type = N'TABLE', @level1name = 'ThirdPartyRequestLogs';
GO

PRINT 'Secondary database schema created successfully';
GO
