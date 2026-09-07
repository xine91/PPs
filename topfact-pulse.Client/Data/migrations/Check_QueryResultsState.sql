USE topfactPulse;
GO

PRINT '=== Spalten von dbo.Pulse_QueryResults ===';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='Pulse_QueryResults'
ORDER BY ORDINAL_POSITION;

PRINT '=== Verwaiste Staging-Spalte vorhanden? ===';
IF EXISTS (SELECT 1 FROM sys.columns
		   WHERE object_id = OBJECT_ID('dbo.Pulse_QueryResults') AND name = 'KategorieID_Int')
	PRINT 'JA - KategorieID_Int existiert noch';
ELSE
	PRINT 'NEIN - keine Staging-Spalte';

PRINT '=== FKs auf Pulse_QueryResults ===';
SELECT fk.name AS FK_Name
FROM sys.foreign_keys fk
JOIN sys.tables t ON fk.parent_object_id = t.object_id
WHERE t.name = 'Pulse_QueryResults';
GO
