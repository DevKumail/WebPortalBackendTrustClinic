-- Add Chat permissions to SecPermission table
-- Run this script on the Primary Database (UEMedical_For_R&D)

USE [UEMedical_For_R&D]
GO

INSERT INTO dbo.SecPermission (PermissionKey, Module, [Name], Description, CreatedBy)
SELECT v.PermissionKey, v.Module, v.[Name], v.Description, 'SYSTEM'
FROM (VALUES
    ('Chat.Read',           'Chat', 'Read Chat',           'View chat conversations and messages'),
    ('Chat.Write',          'Chat', 'Write Chat',          'Send chat messages to patients'),
    ('Chat.SendAttachment', 'Chat', 'Send Attachment',     'Send file attachments in chat messages'),
    ('Chat.BroadcastRead',  'Chat', 'Read Broadcast Chat', 'View broadcast channel conversations'),
    ('Chat.BroadcastWrite', 'Chat', 'Write Broadcast Chat','Send messages in broadcast channels')
) v(PermissionKey, Module, [Name], Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.SecPermission p WHERE p.PermissionKey = v.PermissionKey
);
GO

-- Auto-assign all Chat permissions to Admin role
IF OBJECT_ID('[dbo].[SecRole]', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.SecRolePermission (RoleId, PermissionId, IsAllowed, AssignedBy)
    SELECT r.RoleId, p.PermissionId, 1, 'SYSTEM'
    FROM dbo.SecRole r
    CROSS JOIN dbo.SecPermission p
    WHERE r.Active = 1
      AND r.RoleName = 'Admin'
      AND p.Module = 'Chat'
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.SecRolePermission rp
          WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId
      );
END
GO

PRINT 'Chat permissions seeded successfully.';
GO
