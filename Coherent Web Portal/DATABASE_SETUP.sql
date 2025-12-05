-- =============================================
-- Quick Database Setup Script
-- Run this to verify database connection and check existing tables
-- =============================================

-- Primary Database: UEMedical_For_R&D
-- Secondary Database: CoherentMobApp
-- Server: 175.107.195.221
-- User: Tekno

PRINT '=========================================='
PRINT 'Checking Database Connectivity'
PRINT '=========================================='

-- Check Primary Database
USE [UEMedical_For_R&D]
GO
PRINT 'Connected to Primary Database: UEMedical_For_R&D'
SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName, GETDATE() AS CurrentTime
GO

-- Check if tables already exist
PRINT ''
PRINT 'Existing Tables in Primary Database:'
SELECT name AS TableName 
FROM sys.tables 
ORDER BY name
GO

PRINT ''
PRINT '=========================================='

-- Check Secondary Database
USE [CoherentMobApp]
GO
PRINT 'Connected to Secondary Database: CoherentMobApp'
SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName, GETDATE() AS CurrentTime
GO

-- Check if tables already exist
PRINT ''
PRINT 'Existing Tables in Secondary Database:'
SELECT name AS TableName 
FROM sys.tables 
ORDER BY name
GO

PRINT ''
PRINT '=========================================='
PRINT 'Database Connection Check Complete!'
PRINT '=========================================='
PRINT ''
PRINT 'Next Steps:'
PRINT '1. If Coherent Web Portal tables do not exist, run PrimaryDatabase_Schema.sql'
PRINT '2. Then run PrimaryDatabase_SeedData.sql for initial data'
PRINT '3. Then run SecondaryDatabase_Schema.sql'
PRINT ''
PRINT 'WARNING: If tables already exist with same names, you may need to:'
PRINT '  - Backup existing data'
PRINT '  - Rename existing tables'
PRINT '  - Or use different table names in schema scripts'
PRINT '=========================================='
GO
