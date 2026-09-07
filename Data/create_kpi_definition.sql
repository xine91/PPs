-- Erstellt die KPI-Definitions-Tabelle für topfact Pulse
-- Ausführen auf: topfact_Dashboard (DefaultConnection)

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Pulse_KpiDefinition'
)
BEGIN
    CREATE TABLE [dbo].[Pulse_KpiDefinition] (
        [Id]                 INT            IDENTITY(1,1) NOT NULL,
        [Title]              NVARCHAR(150)  NOT NULL,
        [Description]        NVARCHAR(500)  NULL,
        [IconCss]            NVARCHAR(80)   NOT NULL CONSTRAINT DF_KpiDef_Icon    DEFAULT ('ri-bar-chart-line'),
        [Color]              NVARCHAR(50)   NOT NULL CONSTRAINT DF_KpiDef_Color   DEFAULT ('#338562'),
        [Bereich]            NVARCHAR(100)  NOT NULL CONSTRAINT DF_KpiDef_Bereich DEFAULT (''),
        [DataSource]         NVARCHAR(200)  NULL,
        [QuerySql]           NVARCHAR(2000) NULL,
        [TargetValue]        FLOAT          NULL,
        [ToleranceAbsolute]  FLOAT          NULL,
        [TolerancePercent]   FLOAT          NULL,
        [Unit]               NVARCHAR(30)   NULL,
        [SortOrder]          INT            NOT NULL CONSTRAINT DF_KpiDef_Sort    DEFAULT (10),
        [IsActive]           BIT            NOT NULL CONSTRAINT DF_KpiDef_Active  DEFAULT (1),
        [CreatedAt]          DATETIME2      NOT NULL CONSTRAINT DF_KpiDef_Created DEFAULT (GETUTCDATE()),
        [UpdatedAt]          DATETIME2      NOT NULL CONSTRAINT DF_KpiDef_Updated DEFAULT (GETUTCDATE()),
        [UpdatedBy]          NVARCHAR(150)  NULL,
        CONSTRAINT PK_Pulse_KpiDefinition PRIMARY KEY ([Id])
    );

    PRINT 'Tabelle Pulse_KpiDefinition wurde erstellt.';
END
ELSE
BEGIN
    PRINT 'Tabelle Pulse_KpiDefinition existiert bereits – keine Änderungen.';
END
GO
