-- ============================================================================
-- MIGRATION: Fehlende Foreign Keys + Indexes (Production Ready)
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
    -- 1. FK: Pulse_Gruppe → Pulse_Bereich
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Pulse_Gruppe_Pulse_Bereich'
    )
    BEGIN
        ALTER TABLE dbo.Pulse_Gruppe
        ADD CONSTRAINT FK_Pulse_Gruppe_Pulse_Bereich
            FOREIGN KEY (BereichID)
            REFERENCES dbo.Pulse_Bereich(BereichID)
            ON DELETE CASCADE;

        PRINT 'FK hinzugefügt: Pulse_Gruppe → Pulse_Bereich (CASCADE)';
    END;

    -- =========================================================================
    -- 2. FK: Pulse_Kategorie → Pulse_Gruppe
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Pulse_Kategorie_Pulse_Gruppe'
    )
    BEGIN
        ALTER TABLE dbo.Pulse_Kategorie
        ADD CONSTRAINT FK_Pulse_Kategorie_Pulse_Gruppe
            FOREIGN KEY (GruppeID)
            REFERENCES dbo.Pulse_Gruppe(GruppeID)
            ON DELETE CASCADE;

        PRINT 'FK hinzugefügt: Pulse_Kategorie → Pulse_Gruppe (CASCADE)';
    END;

    -- =========================================================================
    -- 3. FK: Pulse_Access → Pulse_Bereich
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Pulse_Access_Pulse_Bereich'
    )
    BEGIN
        ALTER TABLE dbo.Pulse_Access
        ADD CONSTRAINT FK_Pulse_Access_Pulse_Bereich
            FOREIGN KEY (BereichID)
            REFERENCES dbo.Pulse_Bereich(BereichID);
            -- SQL Server Default = NO ACTION (RESTRICT behavior)

        PRINT 'FK hinzugefügt: Pulse_Access → Pulse_Bereich (NO ACTION)';
    END;

    -- =========================================================================
    -- 4. FK: Pulse_Access → Pulse_Gruppe
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Pulse_Access_Pulse_Gruppe'
    )
    BEGIN
        ALTER TABLE dbo.Pulse_Access
        ADD CONSTRAINT FK_Pulse_Access_Pulse_Gruppe
            FOREIGN KEY (GruppeID)
            REFERENCES dbo.Pulse_Gruppe(GruppeID);
            -- SQL Server Default = NO ACTION (RESTRICT behavior)

        PRINT 'FK hinzugefügt: Pulse_Access → Pulse_Gruppe (NO ACTION)';
    END;

    -- =========================================================================
    -- INDEXES
    -- =========================================================================

    -- Pulse_Bereich (Code)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'UX_Pulse_Bereich_Code'
          AND object_id = OBJECT_ID('dbo.Pulse_Bereich')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_Pulse_Bereich_Code
        ON dbo.Pulse_Bereich(Code);

        PRINT 'Index erstellt: UX_Pulse_Bereich_Code';
    END;

    -- Pulse_Bereich (IsActive)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_Pulse_Bereich_IsActive'
          AND object_id = OBJECT_ID('dbo.Pulse_Bereich')
    )
    BEGIN
        CREATE INDEX IX_Pulse_Bereich_IsActive
        ON dbo.Pulse_Bereich(IsActive);

        PRINT 'Index erstellt: IX_Pulse_Bereich_IsActive';
    END;

    -- Pulse_Gruppe (BereichID, Code)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'UX_Pulse_Gruppe_BereichID_Code'
          AND object_id = OBJECT_ID('dbo.Pulse_Gruppe')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_Pulse_Gruppe_BereichID_Code
        ON dbo.Pulse_Gruppe(BereichID, Code);

        PRINT 'Index erstellt: UX_Pulse_Gruppe_BereichID_Code';
    END;

    -- Pulse_Gruppe (IsActive)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_Pulse_Gruppe_IsActive'
          AND object_id = OBJECT_ID('dbo.Pulse_Gruppe')
    )
    BEGIN
        CREATE INDEX IX_Pulse_Gruppe_IsActive
        ON dbo.Pulse_Gruppe(IsActive);

        PRINT 'Index erstellt: IX_Pulse_Gruppe_IsActive';
    END;

    -- Pulse_Kategorie (GruppeID, Title)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'UX_Pulse_Kategorie_GruppeID_Title'
          AND object_id = OBJECT_ID('dbo.Pulse_Kategorie')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_Pulse_Kategorie_GruppeID_Title
        ON dbo.Pulse_Kategorie(GruppeID, Title);

        PRINT 'Index erstellt: UX_Pulse_Kategorie_GruppeID_Title';
    END;

    -- Pulse_Kategorie (IsActive)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_Pulse_Kategorie_IsActive'
          AND object_id = OBJECT_ID('dbo.Pulse_Kategorie')
    )
    BEGIN
        CREATE INDEX IX_Pulse_Kategorie_IsActive
        ON dbo.Pulse_Kategorie(IsActive);

        PRINT 'Index erstellt: IX_Pulse_Kategorie_IsActive';
    END;

    -- Pulse_Access (user_name, IsActive)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_Pulse_Access_user_name_IsActive'
          AND object_id = OBJECT_ID('dbo.Pulse_Access')
    )
    BEGIN
        CREATE INDEX IX_Pulse_Access_user_name_IsActive
        ON dbo.Pulse_Access(user_name, IsActive);

        PRINT 'Index erstellt: IX_Pulse_Access_user_name_IsActive';
    END;

    -- Pulse_Access (BereichID, GruppeID)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_Pulse_Access_BereichID_GruppeID'
          AND object_id = OBJECT_ID('dbo.Pulse_Access')
    )
    BEGIN
        CREATE INDEX IX_Pulse_Access_BereichID_GruppeID
        ON dbo.Pulse_Access(BereichID, GruppeID);

        PRINT 'Index erstellt: IX_Pulse_Access_BereichID_GruppeID';
    END;

    -- Pulse_Logins (user_name, login_time)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_Pulse_Logins_user_name_login_time'
          AND object_id = OBJECT_ID('dbo.Pulse_Logins')
    )
    BEGIN
        CREATE INDEX IX_Pulse_Logins_user_name_login_time
        ON dbo.Pulse_Logins(user_name, login_time);

        PRINT 'Index erstellt: IX_Pulse_Logins_user_name_login_time';
    END;

    -- Pulse_KpiDefinition (IsActive)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_Pulse_KpiDefinition_IsActive'
          AND object_id = OBJECT_ID('dbo.Pulse_KpiDefinition')
    )
    BEGIN
        CREATE INDEX IX_Pulse_KpiDefinition_IsActive
        ON dbo.Pulse_KpiDefinition(IsActive);

        PRINT 'Index erstellt: IX_Pulse_KpiDefinition_IsActive';
    END;

    -- Pulse_KpiDefinition (Bereich)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_Pulse_KpiDefinition_Bereich'
          AND object_id = OBJECT_ID('dbo.Pulse_KpiDefinition')
    )
    BEGIN
        CREATE INDEX IX_Pulse_KpiDefinition_Bereich
        ON dbo.Pulse_KpiDefinition(Bereich);

        PRINT 'Index erstellt: IX_Pulse_KpiDefinition_Bereich';
    END;

    -- =========================================================================
    -- COMMIT
    -- =========================================================================
    COMMIT TRANSACTION;

    PRINT '';
    PRINT 'Migration erfolgreich abgeschlossen!';
    PRINT '';
    PRINT 'Erstellt/validiert:';
    PRINT '- Foreign Keys (idempotent via sys.foreign_keys)';
    PRINT '- Indexes (idempotent via sys.indexes)';
    PRINT '- SQL Server compliant (NO RESTRICT usage)';
    PRINT '';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT 'FEHLER bei Migration:';
    PRINT @ErrorMessage;

    THROW;
END CATCH;

GO