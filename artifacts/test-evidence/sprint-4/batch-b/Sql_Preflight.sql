SET NOCOUNT ON;
IF DB_NAME() <> N'SyncBridgeCore' THROW 51000, 'Unexpected database target.', 1;
IF CONVERT(nvarchar(40), CONNECTIONPROPERTY('auth_scheme')) = N'SQL' THROW 51001, 'Windows authentication required.', 1;
SELECT CONVERT(nvarchar(32), DB_NAME()) AS DatabaseName, CONVERT(nvarchar(32), CONNECTIONPROPERTY('auth_scheme')) AS Authentication;
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') AND name <> N'__EFMigrationsHistory') <> 15
    THROW 51002, 'Expected exactly fifteen core tables.', 1;
SELECT name AS CoreTable FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') AND name <> N'__EFMigrationsHistory' ORDER BY name;
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261013010705_InitialCreate')
    THROW 51003, 'Accepted migration not found.', 1;
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory;
SELECT COUNT(*) AS ForeignKeyCount, SUM(CASE WHEN delete_referential_action = 0 THEN 1 ELSE 0 END) AS RestrictedDeleteCount
FROM sys.foreign_keys WHERE parent_object_id IN (SELECT object_id FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo'));
SELECT (SELECT COUNT(*) FROM sys.triggers WHERE parent_class = 1) AS DmlTriggers,
    (SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0) AS UserProcedures,
    (SELECT COUNT(*) FROM sys.views WHERE is_ms_shipped = 0) AS UserViews;
SELECT t.name AS TableName, c.name AS ColumnName, TYPE_NAME(c.user_type_id) AS SqlType, c.max_length, c.is_nullable
FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
WHERE (t.name = N'Deviation' AND c.name = N'DeviationCode') OR (t.name = N'AuditEvent' AND c.name = N'AffectedEntityId');
SELECT name AS DeviationCodeIndex, is_unique FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.Deviation') AND name = N'UQ_Deviation_DeviationCode';
SELECT 'AppUser' AS TableName, COUNT_BIG(*) AS [RowCount] FROM dbo.AppUser
UNION ALL SELECT 'Recipe', COUNT_BIG(*) FROM dbo.Recipe
UNION ALL SELECT 'RecipeStep', COUNT_BIG(*) FROM dbo.RecipeStep
UNION ALL SELECT 'RecipeMaterialRequirement', COUNT_BIG(*) FROM dbo.RecipeMaterialRequirement
UNION ALL SELECT 'RecipeEquipmentRequirement', COUNT_BIG(*) FROM dbo.RecipeEquipmentRequirement
UNION ALL SELECT 'Material', COUNT_BIG(*) FROM dbo.Material
UNION ALL SELECT 'Equipment', COUNT_BIG(*) FROM dbo.Equipment
UNION ALL SELECT 'ProductionOrder', COUNT_BIG(*) FROM dbo.ProductionOrder
UNION ALL SELECT 'Batch', COUNT_BIG(*) FROM dbo.Batch
UNION ALL SELECT 'BatchStepExecution', COUNT_BIG(*) FROM dbo.BatchStepExecution
UNION ALL SELECT 'MaterialVerification', COUNT_BIG(*) FROM dbo.MaterialVerification
UNION ALL SELECT 'EquipmentVerification', COUNT_BIG(*) FROM dbo.EquipmentVerification
UNION ALL SELECT 'Deviation', COUNT_BIG(*) FROM dbo.Deviation
UNION ALL SELECT 'ElectronicSignature', COUNT_BIG(*) FROM dbo.ElectronicSignature
UNION ALL SELECT 'AuditEvent', COUNT_BIG(*) FROM dbo.AuditEvent;
