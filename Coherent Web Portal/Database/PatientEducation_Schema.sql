-- =============================================
-- Patient Education Module - Database Schema
-- Version: 1.1
-- Description: Tables for Patient Education feature
-- Note: Single education can have Text+Images, PDF, AND Video (all together)
-- =============================================

-- =============================================
-- Table: MPatientEducationCategory
-- Description: Categories for educational content (e.g., Fertility, Diabetes, General)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducationCategory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MPatientEducationCategory](
        [CategoryId] [int] IDENTITY(1,1) NOT NULL,
        [CategoryName] [nvarchar](200) NOT NULL,
        [ArCategoryName] [nvarchar](200) NULL,
        [CategoryDescription] [nvarchar](500) NULL,
        [ArCategoryDescription] [nvarchar](500) NULL,
        [IconImageName] [nvarchar](255) NULL,
        [DisplayOrder] [int] NULL DEFAULT 0,
        [IsGeneral] [bit] NOT NULL DEFAULT 0,
        [Active] [bit] NOT NULL DEFAULT 1,
        [IsDeleted] [bit] NOT NULL DEFAULT 0,
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] [datetime] NULL,
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        CONSTRAINT [PK_MPatientEducationCategory] PRIMARY KEY CLUSTERED ([CategoryId] ASC)
    )
    
    PRINT 'Table MPatientEducationCategory created successfully.'
END
GO

-- =============================================
-- Table: MPatientEducation
-- Description: Main educational content - can have Text+Images, PDF, AND Video all together
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MPatientEducation]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MPatientEducation](
        [EducationId] [int] IDENTITY(1,1) NOT NULL,
        [CategoryId] [int] NOT NULL,
        [Title] [nvarchar](300) NOT NULL,
        [ArTitle] [nvarchar](300) NULL,
        
        -- Flags to indicate what content is included
        [HasText] [bit] NOT NULL DEFAULT 0,
        [HasPdf] [bit] NOT NULL DEFAULT 0,
        [HasVideo] [bit] NOT NULL DEFAULT 0,
        
        -- Text/HTML Content (like Facility Services) - with inline images in HTML
        [HtmlContent] [nvarchar](MAX) NULL,
        [ArHtmlContent] [nvarchar](MAX) NULL,
        
        -- PDF Document
        [PdfFileName] [nvarchar](255) NULL,
        [PdfFilePath] [nvarchar](500) NULL,
        
        -- Video Content
        [VideoType] [nvarchar](20) NULL, -- 'Upload', 'Link'
        [VideoFileName] [nvarchar](255) NULL,
        [VideoFilePath] [nvarchar](500) NULL,
        [VideoUrl] [nvarchar](1000) NULL, -- YouTube, Facebook, Twitter/X, etc.
        [VideoSource] [nvarchar](50) NULL, -- 'YouTube', 'Facebook', 'Twitter', 'Instagram', 'Other'
        
        -- Thumbnail/Cover Image
        [ThumbnailImageName] [nvarchar](255) NULL,
        
        -- Short Description/Summary
        [Summary] [nvarchar](500) NULL,
        [ArSummary] [nvarchar](500) NULL,
        
        -- Display Settings
        [DisplayOrder] [int] NULL DEFAULT 0,
        [Active] [bit] NOT NULL DEFAULT 1,
        [IsDeleted] [bit] NOT NULL DEFAULT 0,
        
        -- Audit Fields
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] [datetime] NULL,
        [CreatedBy] [int] NULL,
        [UpdatedBy] [int] NULL,
        
        CONSTRAINT [PK_MPatientEducation] PRIMARY KEY CLUSTERED ([EducationId] ASC),
        CONSTRAINT [FK_MPatientEducation_Category] FOREIGN KEY ([CategoryId]) 
            REFERENCES [dbo].[MPatientEducationCategory]([CategoryId])
    )
    
    PRINT 'Table MPatientEducation created successfully.'
END
GO

-- =============================================
-- Table: MPatientEducationImage
-- Description: Multiple images for text content within an education
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
-- Indexes for better query performance
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MPatientEducation_CategoryId' AND object_id = OBJECT_ID('MPatientEducation'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MPatientEducation_CategoryId] 
    ON [dbo].[MPatientEducation]([CategoryId])
    INCLUDE ([Title], [HasText], [HasPdf], [HasVideo], [Active], [IsDeleted])
    
    PRINT 'Index IX_MPatientEducation_CategoryId created successfully.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MPatientEducationCategory_IsGeneral' AND object_id = OBJECT_ID('MPatientEducationCategory'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MPatientEducationCategory_IsGeneral] 
    ON [dbo].[MPatientEducationCategory]([IsGeneral])
    WHERE [IsDeleted] = 0 AND [Active] = 1
    
    PRINT 'Index IX_MPatientEducationCategory_IsGeneral created successfully.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MPatientEducationImage_EducationId' AND object_id = OBJECT_ID('MPatientEducationImage'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MPatientEducationImage_EducationId] 
    ON [dbo].[MPatientEducationImage]([EducationId])
    WHERE [IsDeleted] = 0
    
    PRINT 'Index IX_MPatientEducationImage_EducationId created successfully.'
END
GO

-- =============================================
-- Sample Seed Data (Optional - Comment out if not needed)
-- =============================================
/*
-- Insert General Category
IF NOT EXISTS (SELECT 1 FROM [dbo].[MPatientEducationCategory] WHERE [CategoryName] = 'General')
BEGIN
    INSERT INTO [dbo].[MPatientEducationCategory] 
    ([CategoryName], [ArCategoryName], [CategoryDescription], [IsGeneral], [DisplayOrder], [Active])
    VALUES 
    ('General', N'عام', 'General health education for all patients', 1, 1, 1)
    
    PRINT 'General category seed data inserted.'
END

-- Insert Fertility Category
IF NOT EXISTS (SELECT 1 FROM [dbo].[MPatientEducationCategory] WHERE [CategoryName] = 'Fertility')
BEGIN
    INSERT INTO [dbo].[MPatientEducationCategory] 
    ([CategoryName], [ArCategoryName], [CategoryDescription], [IsGeneral], [DisplayOrder], [Active])
    VALUES 
    ('Fertility', N'الخصوبة', 'Fertility and reproductive health education', 0, 2, 1)
    
    PRINT 'Fertility category seed data inserted.'
END
*/

PRINT 'Patient Education schema setup completed successfully.'
GO
