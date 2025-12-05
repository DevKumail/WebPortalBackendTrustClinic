-- =============================================
-- Coherent Web Portal - Primary Database Schema
-- ADHICS Compliant Database Design
-- Database: UEMedical_For_R&D
-- =============================================

USE [UEMedical_For_R&D]
GO

-- =============================================
-- Users Table
-- =============================================
CREATE TABLE [dbo].[Users] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Username] NVARCHAR(100) NOT NULL UNIQUE,
    [Email] NVARCHAR(255) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(500) NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [PhoneNumber] NVARCHAR(20) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsEmailVerified] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    [LastLoginAt] DATETIME2 NULL,
    [RefreshToken] NVARCHAR(500) NULL,
    [RefreshTokenExpiry] DATETIME2 NULL,
    [CreatedBy] NVARCHAR(100) NOT NULL,
    [UpdatedBy] NVARCHAR(100) NULL,
    INDEX IX_Users_Username NONCLUSTERED ([Username]),
    INDEX IX_Users_Email NONCLUSTERED ([Email]),
    INDEX IX_Users_RefreshToken NONCLUSTERED ([RefreshToken])
);
GO

-- =============================================
-- Roles Table
-- =============================================
CREATE TABLE [dbo].[Roles] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Name] NVARCHAR(100) NOT NULL UNIQUE,
    [Description] NVARCHAR(500) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    INDEX IX_Roles_Name NONCLUSTERED ([Name])
);
GO

-- =============================================
-- Permissions Table
-- =============================================
CREATE TABLE [dbo].[Permissions] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Name] NVARCHAR(100) NOT NULL UNIQUE,
    [Resource] NVARCHAR(100) NOT NULL,
    [Action] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(500) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_Permissions_Name NONCLUSTERED ([Name]),
    INDEX IX_Permissions_Resource NONCLUSTERED ([Resource])
);
GO

-- =============================================
-- UserRoles Junction Table
-- =============================================
CREATE TABLE [dbo].[UserRoles] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [AssignedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [AssignedBy] NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]) ON DELETE CASCADE,
    CONSTRAINT UQ_UserRoles UNIQUE ([UserId], [RoleId]),
    INDEX IX_UserRoles_UserId NONCLUSTERED ([UserId]),
    INDEX IX_UserRoles_RoleId NONCLUSTERED ([RoleId])
);
GO

-- =============================================
-- RolePermissions Junction Table
-- =============================================
CREATE TABLE [dbo].[RolePermissions] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [PermissionId] UNIQUEIDENTIFIER NOT NULL,
    [AssignedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]) ON DELETE CASCADE,
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions]([Id]) ON DELETE CASCADE,
    CONSTRAINT UQ_RolePermissions UNIQUE ([RoleId], [PermissionId]),
    INDEX IX_RolePermissions_RoleId NONCLUSTERED ([RoleId]),
    INDEX IX_RolePermissions_PermissionId NONCLUSTERED ([PermissionId])
);
GO

-- =============================================
-- AuditLogs Table (ADHICS Compliance)
-- =============================================
CREATE TABLE [dbo].[AuditLogs] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [UserId] UNIQUEIDENTIFIER NULL,
    [Username] NVARCHAR(100) NOT NULL,
    [Action] NVARCHAR(100) NOT NULL,
    [EntityType] NVARCHAR(100) NOT NULL,
    [EntityId] NVARCHAR(100) NULL,
    [OldValues] NVARCHAR(MAX) NULL,
    [NewValues] NVARCHAR(MAX) NULL,
    [IpAddress] NVARCHAR(50) NOT NULL,
    [UserAgent] NVARCHAR(500) NOT NULL,
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [AdditionalInfo] NVARCHAR(MAX) NULL,
    [IsSuccess] BIT NOT NULL DEFAULT 1,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [DatabaseSource] NVARCHAR(50) NOT NULL DEFAULT 'Primary',
    [ComplianceCategory] NVARCHAR(50) NOT NULL,
    [RiskLevel] NVARCHAR(20) NOT NULL,
    INDEX IX_AuditLogs_Timestamp NONCLUSTERED ([Timestamp] DESC),
    INDEX IX_AuditLogs_UserId NONCLUSTERED ([UserId]),
    INDEX IX_AuditLogs_Action NONCLUSTERED ([Action]),
    INDEX IX_AuditLogs_RiskLevel NONCLUSTERED ([RiskLevel])
);
GO

-- =============================================
-- Add Comments for ADHICS Compliance
-- =============================================
EXEC sp_addextendedproperty 
    @name = N'ADHICS_Compliance', 
    @value = N'User authentication and authorization table with encrypted password storage', 
    @level0type = N'SCHEMA', @level0name = 'dbo', 
    @level1type = N'TABLE', @level1name = 'Users';
GO

EXEC sp_addextendedproperty 
    @name = N'ADHICS_Compliance', 
    @value = N'Comprehensive audit logging for all system operations', 
    @level0type = N'SCHEMA', @level0name = 'dbo', 
    @level1type = N'TABLE', @level1name = 'AuditLogs';
GO

PRINT 'Primary database schema created successfully';
GO
