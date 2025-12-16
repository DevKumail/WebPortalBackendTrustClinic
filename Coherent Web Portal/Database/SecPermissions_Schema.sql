USE [UEMedical_For_R&D]
GO

IF OBJECT_ID('[dbo].[SecPermission]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SecPermission]
    (
        [PermissionId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PermissionKey] NVARCHAR(150) NOT NULL,
        [Module] NVARCHAR(100) NULL,
        [Description] NVARCHAR(255) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_SecPermission_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_SecPermission_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] NVARCHAR(100) NULL
    );

    CREATE UNIQUE INDEX [UX_SecPermission_PermissionKey] ON [dbo].[SecPermission]([PermissionKey]);
END
GO

IF OBJECT_ID('[dbo].[SecRolePermission]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SecRolePermission]
    (
        [RoleId] INT NOT NULL,
        [PermissionId] INT NOT NULL,
        [IsAllowed] BIT NOT NULL CONSTRAINT [DF_SecRolePermission_IsAllowed] DEFAULT (1),
        [AssignedAt] DATETIME2 NOT NULL CONSTRAINT [DF_SecRolePermission_AssignedAt] DEFAULT (SYSUTCDATETIME()),
        [AssignedBy] NVARCHAR(100) NULL,
        CONSTRAINT [PK_SecRolePermission] PRIMARY KEY CLUSTERED ([RoleId], [PermissionId]),
        CONSTRAINT [FK_SecRolePermission_SecPermission] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[SecPermission]([PermissionId])
    );

    IF OBJECT_ID('[dbo].[SecRole]', 'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[SecRolePermission]
        WITH CHECK ADD CONSTRAINT [FK_SecRolePermission_SecRole] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[SecRole]([RoleId]);
    END
END
GO

IF OBJECT_ID('[dbo].[SecEmployeePermissionOverride]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SecEmployeePermissionOverride]
    (
        [EmpId] BIGINT NOT NULL,
        [PermissionId] INT NOT NULL,
        [IsAllowed] BIT NOT NULL,
        [AssignedAt] DATETIME2 NOT NULL CONSTRAINT [DF_SecEmployeePermissionOverride_AssignedAt] DEFAULT (SYSUTCDATETIME()),
        [AssignedBy] NVARCHAR(100) NULL,
        CONSTRAINT [PK_SecEmployeePermissionOverride] PRIMARY KEY CLUSTERED ([EmpId], [PermissionId]),
        CONSTRAINT [FK_SecEmployeePermissionOverride_SecPermission] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[SecPermission]([PermissionId])
    );

    IF OBJECT_ID('[dbo].[HREmployee]', 'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[SecEmployeePermissionOverride]
        WITH CHECK ADD CONSTRAINT [FK_SecEmployeePermissionOverride_HREmployee] FOREIGN KEY ([EmpId]) REFERENCES [dbo].[HREmployee]([EmpId]);
    END
END
GO
