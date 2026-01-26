-- =============================================
-- Patient Education - Alter Script for Delta JSON
-- Version: 2.0
-- Description: Migrate from HtmlContent to ContentDeltaJson (Quill Delta format)
--              Remove video-related columns, simplify structure
-- =============================================

-- =============================================
-- Step 1: Add new ContentDeltaJson columns
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'ContentDeltaJson')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation]
    ADD [ContentDeltaJson] NVARCHAR(MAX) NULL;
    
    PRINT 'Column ContentDeltaJson added successfully.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'ArContentDeltaJson')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation]
    ADD [ArContentDeltaJson] NVARCHAR(MAX) NULL;
    
    PRINT 'Column ArContentDeltaJson added successfully.'
END
GO

-- =============================================
-- Step 2: Migrate existing HtmlContent to ContentDeltaJson (if needed)
-- Note: This creates a simple Delta JSON wrapper for existing HTML content
-- You may want to do a proper migration on the frontend
-- =============================================
/*
UPDATE [dbo].[MPatientEducation]
SET ContentDeltaJson = CASE 
    WHEN HtmlContent IS NOT NULL AND HtmlContent <> '' 
    THEN '{"ops":[{"insert":"' + REPLACE(REPLACE(REPLACE(HtmlContent, '\', '\\'), '"', '\"'), CHAR(10), '\n') + '\n"}]}'
    ELSE NULL 
END,
ArContentDeltaJson = CASE 
    WHEN ArHtmlContent IS NOT NULL AND ArHtmlContent <> '' 
    THEN '{"ops":[{"insert":"' + REPLACE(REPLACE(REPLACE(ArHtmlContent, '\', '\\'), '"', '\"'), CHAR(10), '\n') + '\n"}]}'
    ELSE NULL 
END
WHERE (HtmlContent IS NOT NULL OR ArHtmlContent IS NOT NULL);

PRINT 'Existing HTML content migrated to Delta JSON format.'
*/

-- =============================================
-- Step 3: Drop indexes and constraints that depend on columns to be dropped
-- =============================================

-- Drop index IX_MPatientEducation_CategoryId (it includes HasText, HasPdf, HasVideo)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MPatientEducation_CategoryId' AND object_id = OBJECT_ID('MPatientEducation'))
BEGIN
    DROP INDEX [IX_MPatientEducation_CategoryId] ON [dbo].[MPatientEducation];
    PRINT 'Index IX_MPatientEducation_CategoryId dropped.'
END
GO

-- Drop default constraints on HasText, HasPdf, HasVideo columns
DECLARE @ConstraintName NVARCHAR(256);

-- Drop default constraint on HasText
SELECT @ConstraintName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('MPatientEducation') AND c.name = 'HasText';

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[MPatientEducation] DROP CONSTRAINT [' + @ConstraintName + ']');
    PRINT 'Default constraint on HasText dropped: ' + @ConstraintName;
END
GO

DECLARE @ConstraintName NVARCHAR(256);

-- Drop default constraint on HasPdf
SELECT @ConstraintName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('MPatientEducation') AND c.name = 'HasPdf';

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[MPatientEducation] DROP CONSTRAINT [' + @ConstraintName + ']');
    PRINT 'Default constraint on HasPdf dropped: ' + @ConstraintName;
END
GO

DECLARE @ConstraintName NVARCHAR(256);

-- Drop default constraint on HasVideo
SELECT @ConstraintName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('MPatientEducation') AND c.name = 'HasVideo';

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[MPatientEducation] DROP CONSTRAINT [' + @ConstraintName + ']');
    PRINT 'Default constraint on HasVideo dropped: ' + @ConstraintName;
END
GO

-- =============================================
-- Step 4: Drop deprecated columns
-- =============================================

-- Drop HasText, HasPdf, HasVideo columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'HasText')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation] DROP COLUMN [HasText];
    PRINT 'Column HasText dropped.'
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'HasPdf')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation] DROP COLUMN [HasPdf];
    PRINT 'Column HasPdf dropped.'
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'HasVideo')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation] DROP COLUMN [HasVideo];
    PRINT 'Column HasVideo dropped.'
END
GO

-- Drop HtmlContent columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'HtmlContent')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation] DROP COLUMN [HtmlContent];
    PRINT 'Column HtmlContent dropped.'
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'ArHtmlContent')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation] DROP COLUMN [ArHtmlContent];
    PRINT 'Column ArHtmlContent dropped.'
END
GO

-- Drop Video-related columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'VideoType')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation] DROP COLUMN [VideoType];
    PRINT 'Column VideoType dropped.'
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'VideoFileName')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation] DROP COLUMN [VideoFileName];
    PRINT 'Column VideoFileName dropped.'
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'VideoFilePath')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation] DROP COLUMN [VideoFilePath];
    PRINT 'Column VideoFilePath dropped.'
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'VideoUrl')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation] DROP COLUMN [VideoUrl];
    PRINT 'Column VideoUrl dropped.'
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND name = 'VideoSource')
BEGIN
    ALTER TABLE [dbo].[MPatientEducation] DROP COLUMN [VideoSource];
    PRINT 'Column VideoSource dropped.'
END
GO

-- =============================================
-- Step 5: Recreate simplified index (without dropped columns)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MPatientEducation_CategoryId' AND object_id = OBJECT_ID('MPatientEducation'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MPatientEducation_CategoryId] 
    ON [dbo].[MPatientEducation]([CategoryId])
    INCLUDE ([Title], [Active], [IsDeleted])
    
    PRINT 'Index IX_MPatientEducation_CategoryId recreated.'
END
GO

PRINT 'Patient Education Delta JSON migration script completed.'
GO
