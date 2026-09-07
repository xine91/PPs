-- ============================================================================
-- MIGRATION: Pulse_QueryResults2 – neue Tabelle mit FK, Index, created_at-Default
-- ============================================================================
-- Idempotent, SQL Server compliant
-- ============================================================================

USE topfactPulse;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
	BEGIN TRANSACTION;

	-- =========================================================================
	-- 1) Tabelle Pulse_QueryResults2 erstellen (falls nicht vorhanden)
	-- =========================================================================
	IF NOT EXISTS (
		SELECT 1 FROM sys.tables
		WHERE SCHEMA_NAME(schema_id) = 'dbo' AND name = 'Pulse_QueryResults2'
	)
	BEGIN
		CREATE TABLE dbo.Pulse_QueryResults2 (
			QueryID INT IDENTITY(1,1) PRIMARY KEY,
			KategorieID INT NULL,
			Sql_query NVARCHAR(MAX) NOT NULL,
			Daten NVARCHAR(MAX) NULL,
			created_at DATETIME2 NOT NULL DEFAULT (GETUTCDATE())
		);

		PRINT 'Tabelle Pulse_QueryResults2 erstellt';
	END;

	-- =========================================================================
	-- 2) FK: Pulse_QueryResults2 → Pulse_Kategorie
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
			ON DELETE SET NULL;

		PRINT 'FK hinzugefügt: Pulse_QueryResults2 → Pulse_Kategorie (SET NULL)';
	END;

	-- =========================================================================
	-- 3) Index auf KategorieID (Index-Only-Scan via INCLUDE)
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

		PRINT 'Index hinzugefügt: IX_Pulse_QueryResults2_KategorieID';
	END;

	COMMIT;
	PRINT 'Migration erfolgreich abgeschlossen';
END TRY
BEGIN CATCH
	ROLLBACK;
	THROW;
END CATCH;
GO
