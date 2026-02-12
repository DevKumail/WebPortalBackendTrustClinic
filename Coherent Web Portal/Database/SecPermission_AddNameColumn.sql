-- Add Name column to SecPermission table
-- Run this script on the Primary Database (UEMedical_For_R&D)

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'SecPermission' AND COLUMN_NAME = 'Name'
)
BEGIN
    ALTER TABLE dbo.SecPermission
    ADD [Name] NVARCHAR(100) NULL;
    
    PRINT 'Name column added to SecPermission table';
END
ELSE
BEGIN
    PRINT 'Name column already exists in SecPermission table';
END
GO

-- Update existing permissions with Name values based on PermissionKey
UPDATE dbo.SecPermission SET [Name] = 'Manage Roles' WHERE PermissionKey = 'Roles.Manage';
UPDATE dbo.SecPermission SET [Name] = 'Read Permissions' WHERE PermissionKey = 'Permissions.Read';
UPDATE dbo.SecPermission SET [Name] = 'Read Employees' WHERE PermissionKey = 'Employees.Read';
UPDATE dbo.SecPermission SET [Name] = 'Read Doctors' WHERE PermissionKey = 'Doctors.Read';
UPDATE dbo.SecPermission SET [Name] = 'Manage Doctors' WHERE PermissionKey = 'Doctors.Manage';
UPDATE dbo.SecPermission SET [Name] = 'Read Facilities' WHERE PermissionKey = 'Facilities.Read';
UPDATE dbo.SecPermission SET [Name] = 'Manage Facilities' WHERE PermissionKey = 'Facilities.Manage';
UPDATE dbo.SecPermission SET [Name] = 'Manage Doctor Facilities' WHERE PermissionKey = 'DoctorFacilities.Manage';
UPDATE dbo.SecPermission SET [Name] = 'Read Specialities' WHERE PermissionKey = 'Specialities.Read';
UPDATE dbo.SecPermission SET [Name] = 'Manage Specialities' WHERE PermissionKey = 'Specialities.Manage';
UPDATE dbo.SecPermission SET [Name] = 'Read Facility Services' WHERE PermissionKey = 'FacilityServices.Read';
UPDATE dbo.SecPermission SET [Name] = 'Manage Facility Services' WHERE PermissionKey = 'FacilityServices.Manage';
UPDATE dbo.SecPermission SET [Name] = 'Read Patient Education' WHERE PermissionKey = 'PatientEducation.Read';
UPDATE dbo.SecPermission SET [Name] = 'Manage Patient Education' WHERE PermissionKey = 'PatientEducation.Manage';
UPDATE dbo.SecPermission SET [Name] = 'Read Promotions' WHERE PermissionKey = 'Promotions.Read';
UPDATE dbo.SecPermission SET [Name] = 'Manage Promotions' WHERE PermissionKey = 'Promotions.Manage';

PRINT 'Existing permissions updated with Name values';
GO
