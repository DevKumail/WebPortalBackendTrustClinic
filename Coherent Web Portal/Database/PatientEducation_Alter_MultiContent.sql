-- =============================================
-- Patient Education Module - ALTER Script
-- Version: 1.1
-- Description: Modify existing tables to support multiple content types
-- Run this AFTER the original PatientEducation_Schema.sql
-- =============================================

-- =============================================
-- Step 1: Add new columns to MPatientEducation table
-- HasText, HasPdf, HasVideo flags (can have all three together)
-- =============================================

-- Add HasText column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'HasText')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation]
    ADD [HasText] [bit] NOT NULL DEFAULT 0
    
    PRINT 'Column HasText added to MPatientEducation.'
END
GO

-- Add HasPdf column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'HasPdf')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation]
    ADD [HasPdf] [bit] NOT NULL DEFAULT 0
    
    PRINT 'Column HasPdf added to MPatientEducation.'
END
GO

-- Add HasVideo column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'HasVideo')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation]
    ADD [HasVideo] [bit] NOT NULL DEFAULT 0
    
    PRINT 'Column HasVideo added to MPatientEducation.'
END
GO

-- =============================================
-- Step 2: Migrate data from ContentType to new flags (if ContentType exists)
-- =============================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'ContentType')
BEGIN
    -- Set HasText = 1 where ContentType = 'Text'
    UPDATE [dbo].[MPatientEducation]
    SET HasText = 1
    WHERE ContentType = 'Text'
    
    -- Set HasPdf = 1 where ContentType = 'PDF'
    UPDATE [dbo].[MPatientEducation]
    SET HasPdf = 1
    WHERE ContentType = 'PDF'
    
    -- Set HasVideo = 1 where ContentType = 'Video'
    UPDATE [dbo].[MPatientEducation]
    SET HasVideo = 1
    WHERE ContentType = 'Video'
    
    PRINT 'Data migrated from ContentType to new flag columns.'
END
GO

-- =============================================
-- Step 3: Drop ContentType column (optional - uncomment if you want to remove it)
-- =============================================
/*
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'ContentType')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation]
    DROP COLUMN [ContentType]
    
    PRINT 'Column ContentType dropped from MPatientEducation.'
END
GO
*/

-- =============================================
-- Step 4: Create MPatientEducationImage table (NEW table for multiple images)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducationImage]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MPatientEducationImage](
        [ImageId] [int] IDENTITY(1,1) NOT NULL,
        [EducationId] [int] NOT NULL,
        [ImageFileName] [nvarchar](255) NOT NULL,
        [ImageCaption] [nvarchar](300) NULL,
        [ArImageCaption] [nvarchar](300) NULL,
        [DisplayOrder] [int] NULL DEFAULT 0,
        [IsDeleted] [bit] NOT NULL DEFAULT 0,
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT [PK_MPatientEducationImage] PRIMARY KEY CLUSTERED ([ImageId] ASC),
        CONSTRAINT [FK_MPatientEducationImage_Education] FOREIGN KEY ([EducationId]) 
            REFERENCES [dbo].[MPatientEducation]([EducationId])
    )
    
    PRINT 'Table MPatientEducationImage created successfully.'
END
GO

-- =============================================
-- Step 5: Create index on MPatientEducationImage
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MPatientEducationImage_EducationId' AND object_id = OBJECT_ID('MPatientEducationImage'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MPatientEducationImage_EducationId] 
    ON [dbo].[MPatientEducationImage]([EducationId])
    WHERE [IsDeleted] = 0
    
    PRINT 'Index IX_MPatientEducationImage_EducationId created successfully.'
END
GO

-- =============================================
-- Step 6: Update index on MPatientEducation (drop old, create new)
-- =============================================
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MPatientEducation_ContentType' AND object_id = OBJECT_ID('MPatientEducation'))
BEGIN
    DROP INDEX [IX_MPatientEducation_ContentType] ON [dbo].[MPatientEducation]
    PRINT 'Index IX_MPatientEducation_ContentType dropped.'
END
GO

-- Update CategoryId index to include new columns
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MPatientEducation_CategoryId' AND object_id = OBJECT_ID('MPatientEducation'))
BEGIN
    DROP INDEX [IX_MPatientEducation_CategoryId] ON [dbo].[MPatientEducation]
    PRINT 'Old index IX_MPatientEducation_CategoryId dropped.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MPatientEducation_CategoryId' AND object_id = OBJECT_ID('MPatientEducation'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MPatientEducation_CategoryId] 
    ON [dbo].[MPatientEducation]([CategoryId])
    INCLUDE ([Title], [HasText], [HasPdf], [HasVideo], [Active], [IsDeleted])
    
    PRINT 'Index IX_MPatientEducation_CategoryId created with new columns.'
END
GO

PRINT 'Patient Education ALTER script completed successfully.'
GO
