-- ============================================================================
-- MIGRATION: Pulse_QueryResults – FK, Index, created_at-Default
-- ============================================================================
USE topfactPulse;
GO

SET NOCOUNT ON;

-- Schritt 1: FK hinzufügen (falls nicht vorhanden)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Pulse_QueryResults_Pulse_Kategorie')
BEGIN
	ALTER TABLE dbo.Pulse_QueryResults
	ADD CONSTRAINT FK_Pulse_QueryResults_Pulse_Kategorie
	FOREIGN KEY (KategorieID) REFERENCES dbo.Pulse_Kategorie(KategorieID);
	PRINT 'FK_Pulse_QueryResults_Pulse_Kategorie erstellt';
END
ELSE
	PRINT 'FK_Pulse_QueryResults_Pulse_Kategorie existiert bereits';

GO

-- Schritt 2: Index hinzufügen (falls nicht vorhanden)
IF NOT EXISTS (SELECT 1 FROM sys.indexes 
			   WHERE name = 'IX_Pulse_QueryResults_KategorieID' 
			   AND object_id = OBJECT_ID('dbo.Pulse_QueryResults'))
BEGIN
	CREATE NONCLUSTERED INDEX IX_Pulse_QueryResults_KategorieID
	ON dbo.Pulse_QueryResults (KategorieID)
	INCLUDE (Sql_query, Daten, created_at);
	PRINT 'IX_Pulse_QueryResults_KategorieID erstellt';
END
ELSE
	PRINT 'IX_Pulse_QueryResults_KategorieID existiert bereits';

GO

-- Schritt 3: Default für created_at hinzufügen (falls nicht vorhanden)
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints 
			   WHERE name = 'DF_Pulse_QueryResults_created_at'
			   AND parent_object_id = OBJECT_ID('dbo.Pulse_QueryResults'))
BEGIN
	ALTER TABLE dbo.Pulse_QueryResults
	ADD CONSTRAINT DF_Pulse_QueryResults_created_at DEFAULT (GETUTCDATE()) FOR created_at;
	PRINT 'Default DF_Pulse_QueryResults_created_at erstellt';
END
ELSE
	PRINT 'Default DF_Pulse_QueryResults_created_at existiert bereits';

GO
