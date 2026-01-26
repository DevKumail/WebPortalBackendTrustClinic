-- =============================================
-- Patient Education Assignment - Database Schema
-- Version: 1.0
-- Description: Table to assign education content to specific patients
-- =============================================

-- =============================================
-- Table: TPatientEducationAssignment
-- Description: Links education content to specific patients
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TPatientEducationAssignment]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TPatientEducationAssignment](
        [AssignmentId] [int] IDENTITY(1,1) NOT NULL,
        [PatientId] [int] NOT NULL,
        [EducationId] [int] NOT NULL,
        
        -- Assignment details
        [AssignedByUserId] [int] NULL,
        [AssignedAt] [datetime] NOT NULL DEFAULT GETDATE(),
        [Notes] [nvarchar](500) NULL,
        [ArNotes] [nvarchar](500) NULL,
        
        -- Patient viewing status
        [IsViewed] [bit] NOT NULL DEFAULT 0,
        [ViewedAt] [datetime] NULL,
        
        -- Expiry (optional - if education should be available only for limited time)
        [ExpiresAt] [datetime] NULL,
        
        -- Status
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [IsDeleted] [bit] NOT NULL DEFAULT 0,
        
        -- Audit
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] [datetime] NULL,
        
        CONSTRAINT [PK_TPatientEducationAssignment] PRIMARY KEY CLUSTERED ([AssignmentId] ASC),
        CONSTRAINT [FK_TPatientEducationAssignment_Education] FOREIGN KEY ([EducationId]) 
            REFERENCES [dbo].[MPatientEducation]([EducationId])
    )
    
    PRINT 'Table TPatientEducationAssignment created successfully.'
END
GO

-- =============================================
-- Indexes for better query performance
-- =============================================

-- Index for finding assignments by patient
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TPatientEducationAssignment_PatientId' AND object_id = OBJECT_ID('TPatientEducationAssignment'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TPatientEducationAssignment_PatientId] 
    ON [dbo].[TPatientEducationAssignment]([PatientId])
    INCLUDE ([EducationId], [IsViewed], [IsActive])
    WHERE [IsDeleted] = 0
    
    PRINT 'Index IX_TPatientEducationAssignment_PatientId created successfully.'
END
GO

-- Index for finding assignments by education
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TPatientEducationAssignment_EducationId' AND object_id = OBJECT_ID('TPatientEducationAssignment'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TPatientEducationAssignment_EducationId] 
    ON [dbo].[TPatientEducationAssignment]([EducationId])
    WHERE [IsDeleted] = 0
    
    PRINT 'Index IX_TPatientEducationAssignment_EducationId created successfully.'
END
GO

-- Unique constraint to prevent duplicate assignments
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TPatientEducationAssignment_Unique' AND object_id = OBJECT_ID('TPatientEducationAssignment'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_TPatientEducationAssignment_Unique] 
    ON [dbo].[TPatientEducationAssignment]([PatientId], [EducationId])
    WHERE [IsDeleted] = 0
    
    PRINT 'Unique index IX_TPatientEducationAssignment_Unique created successfully.'
END
GO

PRINT 'PatientEducationAssignment schema completed successfully.'
GO
