-- Migration: Add IsMobileUser column to RegPatient table
-- Purpose: Track if patient has registered on the mobile app
-- Date: 2024-12-29

-- Add IsMobileUser column to RegPatient table
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'RegPatient' 
    AND COLUMN_NAME = 'IsMobileUser'
)
BEGIN
    ALTER TABLE RegPatient
    ADD IsMobileUser BIT NULL DEFAULT 0;
    
    PRINT 'Column IsMobileUser added to RegPatient table successfully.';
END
ELSE
BEGIN
    PRINT 'Column IsMobileUser already exists in RegPatient table.';
END
GO

-- Create index on IsMobileUser for faster filtering
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_RegPatient_IsMobileUser' 
    AND object_id = OBJECT_ID('RegPatient')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RegPatient_IsMobileUser
    ON RegPatient (IsMobileUser);
    
    PRINT 'Index IX_RegPatient_IsMobileUser created successfully.';
END
ELSE
BEGIN
    PRINT 'Index IX_RegPatient_IsMobileUser already exists.';
END
GO
