-- Add IsCRM column to HREmployee table
-- Run this on UEMedical_For_R&D database

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HREmployee') AND name = 'IsCRM')
BEGIN
    ALTER TABLE dbo.HREmployee
    ADD IsCRM BIT NOT NULL DEFAULT 0;
    
    PRINT 'IsCRM column added to HREmployee table';
END
ELSE
BEGIN
    PRINT 'IsCRM column already exists';
END
GO
