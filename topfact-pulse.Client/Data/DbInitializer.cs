using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace topfact.Pulse.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeDatabaseAsync(AppDbContext context)
        {
            try
            {
                await context.Database.EnsureCreatedAsync();
                await EnsurePulseQueryResults2TableAsync(context);
                Debug.WriteLine("[DbInitializer] Datenbank initialisiert");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DbInitializer] Fehler: {ex.Message}");
            }
        }

        private static async Task EnsurePulseQueryResults2TableAsync(AppDbContext context)
        {
            try
            {
                var sql = @"
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
                            created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                        );
                    END

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
                    END

                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes 
                        WHERE name = 'IX_Pulse_QueryResults2_KategorieID'
                    )
                    BEGIN
                        CREATE INDEX IX_Pulse_QueryResults2_KategorieID 
                        ON dbo.Pulse_QueryResults2(KategorieID);
                    END
                ";

                await context.Database.ExecuteSqlRawAsync(sql);
                Debug.WriteLine("[DbInitializer] Pulse_QueryResults2 Tabelle OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DbInitializer] Tabelleninitialisierung Fehler: {ex.Message}");
            }
        }
    }
}
