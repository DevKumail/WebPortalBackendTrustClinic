-- =============================================
-- Promotions Module - Database Schema
-- Version: 1.0
-- Description: Promotional banners/sliders for mobile app
-- =============================================

-- =============================================
-- Table: MPromotion
-- Description: Stores promotional banner images for mobile slider
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MPromotion]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MPromotion](
        [PromotionId] [int] IDENTITY(1,1) NOT NULL,
        
        -- Basic Info
        [Title] [nvarchar](200) NOT NULL,
        [ArTitle] [nvarchar](200) NULL,
        [Description] [nvarchar](500) NULL,
        [ArDescription] [nvarchar](500) NULL,
        
        -- Image
        [ImageFileName] [nvarchar](255) NOT NULL,
        
        -- Link URL (where to navigate when clicked)
        [LinkUrl] [nvarchar](500) NULL,
        [LinkType] [nvarchar](50) NULL, -- Internal, External, None
        
        -- Display settings
        [DisplayOrder] [int] NOT NULL DEFAULT 0,
        [StartDate] [datetime] NULL,
        [EndDate] [datetime] NULL,
        
        -- Status
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [IsDeleted] [bit] NOT NULL DEFAULT 0,
        
        -- Audit
        [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
        [CreatedBy] [int] NULL,
        [UpdatedAt] [datetime] NULL,
        [UpdatedBy] [int] NULL,
        
        CONSTRAINT [PK_MPromotion] PRIMARY KEY CLUSTERED ([PromotionId] ASC)
    )
    
    PRINT 'Table MPromotion created successfully.'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index for active promotions query
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MPromotion_Active' AND object_id = OBJECT_ID('MPromotion'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MPromotion_Active] 
    ON [dbo].[MPromotion]([IsActive], [DisplayOrder])
    INCLUDE ([Title], [ImageFileName], [LinkUrl], [StartDate], [EndDate])
    WHERE [IsDeleted] = 0
    
    PRINT 'Index IX_MPromotion_Active created successfully.'
END
GO

PRINT 'Promotions schema completed successfully.'
GO
