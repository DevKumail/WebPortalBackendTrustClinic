-- =============================================
-- Coherent Web Portal - Seed Data
-- Initial Roles, Permissions, and Admin User
-- Database: UEMedical_For_R&D
-- =============================================

USE [UEMedical_For_R&D]
GO

-- =============================================
-- Insert Default Roles
-- =============================================
DECLARE @AdminRoleId UNIQUEIDENTIFIER = NEWID();
DECLARE @DoctorRoleId UNIQUEIDENTIFIER = NEWID();
DECLARE @NurseRoleId UNIQUEIDENTIFIER = NEWID();
DECLARE @AuditorRoleId UNIQUEIDENTIFIER = NEWID();
DECLARE @UserRoleId UNIQUEIDENTIFIER = NEWID();

INSERT INTO [dbo].[Roles] ([Id], [Name], [Description], [IsActive], [CreatedAt])
VALUES 
    (@AdminRoleId, 'Admin', 'System administrator with full access', 1, GETUTCDATE()),
    (@DoctorRoleId, 'Doctor', 'Healthcare provider with patient data access', 1, GETUTCDATE()),
    (@NurseRoleId, 'Nurse', 'Healthcare staff with limited patient data access', 1, GETUTCDATE()),
    (@AuditorRoleId, 'Auditor', 'Compliance auditor with read-only audit log access', 1, GETUTCDATE()),
    (@UserRoleId, 'User', 'Standard user with basic access', 1, GETUTCDATE());

PRINT 'Roles created successfully';

-- =============================================
-- Insert Default Permissions
-- =============================================
DECLARE @PermissionIds TABLE (Id UNIQUEIDENTIFIER, Name NVARCHAR(100));

INSERT INTO [dbo].[Permissions] ([Id], [Name], [Resource], [Action], [Description], [CreatedAt])
OUTPUT INSERTED.Id, INSERTED.Name INTO @PermissionIds
VALUES 
    (NEWID(), 'Users.Create', 'Users', 'Create', 'Create new users', GETUTCDATE()),
    (NEWID(), 'Users.Read', 'Users', 'Read', 'View user information', GETUTCDATE()),
    (NEWID(), 'Users.Update', 'Users', 'Update', 'Update user information', GETUTCDATE()),
    (NEWID(), 'Users.Delete', 'Users', 'Delete', 'Delete users', GETUTCDATE()),
    (NEWID(), 'Roles.Manage', 'Roles', 'Manage', 'Manage roles and permissions', GETUTCDATE()),
    (NEWID(), 'AuditLogs.Read', 'AuditLogs', 'Read', 'View audit logs', GETUTCDATE()),
    (NEWID(), 'Patients.Create', 'Patients', 'Create', 'Create patient records', GETUTCDATE()),
    (NEWID(), 'Patients.Read', 'Patients', 'Read', 'View patient records', GETUTCDATE()),
    (NEWID(), 'Patients.Update', 'Patients', 'Update', 'Update patient records', GETUTCDATE()),
    (NEWID(), 'Patients.Delete', 'Patients', 'Delete', 'Delete patient records', GETUTCDATE());

PRINT 'Permissions created successfully';

-- =============================================
-- Assign Permissions to Admin Role (Full Access)
-- =============================================
INSERT INTO [dbo].[RolePermissions] ([Id], [RoleId], [PermissionId], [AssignedAt])
SELECT NEWID(), @AdminRoleId, Id, GETUTCDATE()
FROM [dbo].[Permissions];

PRINT 'Admin permissions assigned successfully';

-- =============================================
-- Assign Permissions to Doctor Role
-- =============================================
INSERT INTO [dbo].[RolePermissions] ([Id], [RoleId], [PermissionId], [AssignedAt])
SELECT NEWID(), @DoctorRoleId, p.Id, GETUTCDATE()
FROM [dbo].[Permissions] p
WHERE p.Name IN ('Patients.Create', 'Patients.Read', 'Patients.Update', 'Users.Read');

PRINT 'Doctor permissions assigned successfully';

-- =============================================
-- Assign Permissions to Nurse Role
-- =============================================
INSERT INTO [dbo].[RolePermissions] ([Id], [RoleId], [PermissionId], [AssignedAt])
SELECT NEWID(), @NurseRoleId, p.Id, GETUTCDATE()
FROM [dbo].[Permissions] p
WHERE p.Name IN ('Patients.Read', 'Patients.Update', 'Users.Read');

PRINT 'Nurse permissions assigned successfully';

-- =============================================
-- Assign Permissions to Auditor Role
-- =============================================
INSERT INTO [dbo].[RolePermissions] ([Id], [RoleId], [PermissionId], [AssignedAt])
SELECT NEWID(), @AuditorRoleId, p.Id, GETUTCDATE()
FROM [dbo].[Permissions] p
WHERE p.Name IN ('AuditLogs.Read', 'Users.Read');

PRINT 'Auditor permissions assigned successfully';

-- =============================================
-- Assign Permissions to User Role
-- =============================================
INSERT INTO [dbo].[RolePermissions] ([Id], [RoleId], [PermissionId], [AssignedAt])
SELECT NEWID(), @UserRoleId, p.Id, GETUTCDATE()
FROM [dbo].[Permissions] p
WHERE p.Name IN ('Users.Read');

PRINT 'User permissions assigned successfully';

-- =============================================
-- Create Default Admin User
-- Password: Admin@123 (BCrypt hashed)
-- IMPORTANT: Change this password in production!
-- =============================================
DECLARE @AdminUserId UNIQUEIDENTIFIER = NEWID();

INSERT INTO [dbo].[Users] 
([Id], [Username], [Email], [PasswordHash], [FirstName], [LastName], [PhoneNumber], 
 [IsActive], [IsEmailVerified], [CreatedAt], [CreatedBy])
VALUES 
(@AdminUserId, 
 'admin', 
 'admin@coherent.local', 
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIz.9GEjVa', -- Admin@123
 'System', 
 'Administrator', 
 '+971501234567',
 1, 
 1, 
 GETUTCDATE(), 
 'SYSTEM');

-- Assign Admin Role to Admin User
INSERT INTO [dbo].[UserRoles] ([Id], [UserId], [RoleId], [AssignedAt], [AssignedBy])
VALUES (NEWID(), @AdminUserId, @AdminRoleId, GETUTCDATE(), 'SYSTEM');

PRINT 'Admin user created successfully';
PRINT 'Username: admin';
PRINT 'Password: Admin@123';
PRINT 'IMPORTANT: Change the admin password immediately after first login!';

-- =============================================
-- Create Sample Users
-- =============================================
DECLARE @DoctorUserId UNIQUEIDENTIFIER = NEWID();
DECLARE @NurseUserId UNIQUEIDENTIFIER = NEWID();

INSERT INTO [dbo].[Users] 
([Id], [Username], [Email], [PasswordHash], [FirstName], [LastName], [PhoneNumber], 
 [IsActive], [IsEmailVerified], [CreatedAt], [CreatedBy])
VALUES 
(@DoctorUserId, 'doctor1', 'doctor1@coherent.local', 
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIz.9GEjVa', 
 'John', 'Doe', '+971501234568', 1, 1, GETUTCDATE(), 'admin'),
 
(@NurseUserId, 'nurse1', 'nurse1@coherent.local', 
 '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIz.9GEjVa', 
 'Jane', 'Smith', '+971501234569', 1, 1, GETUTCDATE(), 'admin');

INSERT INTO [dbo].[UserRoles] ([Id], [UserId], [RoleId], [AssignedAt], [AssignedBy])
VALUES 
(NEWID(), @DoctorUserId, @DoctorRoleId, GETUTCDATE(), 'admin'),
(NEWID(), @NurseUserId, @NurseRoleId, GETUTCDATE(), 'admin');

PRINT 'Sample users created successfully';
PRINT 'All sample users have password: Admin@123';

GO

PRINT '========================================';
PRINT 'Seed data created successfully!';
PRINT '========================================';
PRINT 'Default Credentials:';
PRINT 'Admin: admin / Admin@123';
PRINT 'Doctor: doctor1 / Admin@123';
PRINT 'Nurse: nurse1 / Admin@123';
PRINT '========================================';
PRINT 'SECURITY WARNING: Change all default passwords in production!';
PRINT '========================================';
GO
