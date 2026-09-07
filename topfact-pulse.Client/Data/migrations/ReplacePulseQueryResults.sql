-- ============================================================================
-- MIGRATION: Ersetze Pulse_QueryResults durch Pulse_QueryResults2
-- ============================================================================
-- 1. Alte Tabelle droppen (mit FKs)
-- 2. Neue Tabelle Pulse_QueryResults2 als Pulse_QueryResults umbenennen
-- ============================================================================

USE topfactPulse;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
	BEGIN TRANSACTION;

	-- =========================================================================
	-- 1) Alte FKs, Indizes, Defaults auf Pulse_QueryResults droppen
	-- =========================================================================
	IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Pulse_QueryResults_Pulse_Kategorie')
		ALTER TABLE dbo.Pulse_QueryResults DROP CONSTRAINT FK_Pulse_QueryResults_Pulse_Kategorie;

	IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pulse_QueryResults_KategorieID' 
			   AND object_id = OBJECT_ID('dbo.Pulse_QueryResults'))
		DROP INDEX IX_Pulse_QueryResults_KategorieID ON dbo.Pulse_QueryResults;

	DECLARE @dfName SYSNAME;
	SELECT @dfName = dc.name
	FROM sys.default_constraints dc
	JOIN sys.columns c ON c.default_object_id = dc.object_id
	WHERE c.object_id = OBJECT_ID('dbo.Pulse_QueryResults') AND c.name = 'created_at';
	IF @dfName IS NOT NULL
		EXEC('ALTER TABLE dbo.Pulse_QueryResults DROP CONSTRAINT ' + @dfName);

	-- =========================================================================
	-- 2) Alte Tabelle droppen
	-- =========================================================================
	DROP TABLE dbo.Pulse_QueryResults;
	PRINT 'Alte Tabelle Pulse_QueryResults gelöscht';

	-- =========================================================================
	-- 3) Falls Pulse_QueryResults2 nicht existiert, erstellen
	-- =========================================================================
	IF NOT EXISTS (
		SELECT 1 FROM sys.tables
		WHERE SCHEMA_NAME(schema_id) = 'dbo' AND name = 'Pulse_QueryResults2'
	)
	BEGIN
		CREATE TABLE dbo.Pulse_QueryResults2 (
			QueryID INT IDENTITY(1,1) PRIMARY KEY,
			KategorieID INT NOT NULL,
			Sql_query NVARCHAR(2000) NULL,
			Daten NVARCHAR(MAX) NULL,
			created_at DATETIME2 NOT NULL DEFAULT (GETUTCDATE())
		);

		PRINT 'Neue Tabelle Pulse_QueryResults2 erstellt';
	END;

	-- =========================================================================
	-- 4) FK hinzufügen
	-- =========================================================================
	IF NOT EXISTS (
		SELECT 1 FROM sys.foreign_keys
		WHERE name = 'FK_Pulse_QueryResults2_Pulse_Kategorie'
	)
	BEGIN
		ALTER TABLE dbo.Pulse_QueryResults2
		ADD CONSTRAINT FK_Pulse_QueryResults2_Pulse_Kategorie
			FOREIGN KEY (KategorieID)
			REFERENCES dbo.Pulse_Kategorie(KategorieID)
			ON DELETE CASCADE;

		PRINT 'FK hinzugefügt';
	END;

	-- =========================================================================
	-- 5) Index hinzufügen
	-- =========================================================================
	IF NOT EXISTS (
		SELECT 1 FROM sys.indexes
		WHERE name = 'IX_Pulse_QueryResults2_KategorieID'
		  AND object_id = OBJECT_ID('dbo.Pulse_QueryResults2')
	)
	BEGIN
		CREATE NONCLUSTERED INDEX IX_Pulse_QueryResults2_KategorieID
		ON dbo.Pulse_QueryResults2 (KategorieID)
		INCLUDE (Sql_query, created_at);

		PRINT 'Index hinzugefügt';
	END;

	-- =========================================================================
	-- 6) Pulse_QueryResults2 zu Pulse_QueryResults umbenennen
	-- =========================================================================
	EXEC sp_rename 'dbo.Pulse_QueryResults2', 'Pulse_QueryResults', 'OBJECT';
	EXEC sp_rename 'dbo.PK__Pulse_Qu__3214EC0745F7C5FB', 'PK_Pulse_QueryResults', 'INDEX';
	EXEC sp_rename 'dbo.FK_Pulse_QueryResults2_Pulse_Kategorie', 'FK_Pulse_QueryResults_Pulse_Kategorie', 'OBJECT';
	EXEC sp_rename 'dbo.IX_Pulse_QueryResults2_KategorieID', 'IX_Pulse_QueryResults_KategorieID', 'OBJECT';

	PRINT 'Tabelle zu Pulse_QueryResults umbenannt';

	COMMIT;
	PRINT 'Migration erfolgreich abgeschlossen';
END TRY
BEGIN CATCH
	ROLLBACK;
	THROW;
END CATCH;
GO
