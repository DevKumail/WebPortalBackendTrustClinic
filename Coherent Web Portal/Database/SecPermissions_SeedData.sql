USE [UEMedical_For_R&D]
GO

INSERT INTO dbo.SecPermission (PermissionKey, Module, Description, CreatedBy)
SELECT v.PermissionKey, v.Module, v.Description, 'SYSTEM'
FROM (VALUES
    ('Roles.Manage', 'Security', 'Manage roles and permissions'),
    ('Permissions.Read', 'Security', 'View permissions'),
    ('Employees.Read', 'Employees', 'View employees'),
    ('Doctors.Read', 'CRM', 'View doctors (SecondaryDatabase)'),
    ('Doctors.Manage', 'CRM', 'Create/update doctors (SecondaryDatabase)'),
    ('Facilities.Read', 'CRM', 'View facilities (SecondaryDatabase)'),
    ('Facilities.Manage', 'CRM', 'Create/update facilities (SecondaryDatabase)'),
    ('DoctorFacilities.Manage', 'CRM', 'Manage doctor-facility mapping (SecondaryDatabase)'),
    ('Appointments.Read', 'Appointments', 'View appointments'),
    ('Appointments.Book', 'Appointments', 'Book appointment'),
    ('Appointments.Cancel', 'Appointments', 'Cancel appointment'),
    ('Appointments.Reschedule', 'Appointments', 'Reschedule appointment'),
    ('PatientEducation.Read', 'PatientEducation', 'View patient education content'),
    ('PatientEducation.Manage', 'PatientEducation', 'Create/update/delete patient education content'),
    ('Promotions.Read', 'Promotions', 'View promotions/banners'),
    ('Promotions.Manage', 'Promotions', 'Create/update/delete promotions/banners'),
    ('Patients.Read', 'Patients', 'View patient records'),
    ('FacilityServices.Read', 'CRM', 'View facility services'),
    ('FacilityServices.Manage', 'CRM', 'Create/update facility services'),
    ('Specialities.Read', 'CRM', 'View specialities'),
    ('Specialities.Manage', 'CRM', 'Create/update specialities'),
    ('Chat.Read', 'Chat', 'View chat conversations and messages'),
    ('Chat.Write', 'Chat', 'Send chat messages to patients'),
    ('Chat.SendAttachment', 'Chat', 'Send file attachments in chat messages'),
    ('Chat.BroadcastRead', 'Chat', 'View broadcast channel conversations'),
    ('Chat.BroadcastWrite', 'Chat', 'Send messages in broadcast channels')
) v(PermissionKey, Module, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.SecPermission p WHERE p.PermissionKey = v.PermissionKey
);
GO

IF OBJECT_ID('[dbo].[SecRole]', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.SecRolePermission (RoleId, PermissionId, IsAllowed, AssignedBy)
    SELECT r.RoleId, p.PermissionId, 1, 'SYSTEM'
    FROM dbo.SecRole r
    CROSS JOIN dbo.SecPermission p
    WHERE r.Active = 1
      AND r.RoleName = 'Admin'
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.SecRolePermission rp
          WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId
      );
END
GO
