USE [topfact_Dashboard];
GO

/*
    UI Navigation (3NF) - Create + Seed Script
    Für: topfact.Pulse
    Idempotent: Ja (mehrfach ausführbar)
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* =========================================================
       1) Tabellen anlegen (falls nicht vorhanden)
       ========================================================= */

    IF OBJECT_ID(N'dbo.UiMode', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.UiMode
        (
            UiModeId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UiMode PRIMARY KEY,
            Code            NVARCHAR(50)  NOT NULL,
            DisplayName     NVARCHAR(100) NOT NULL,
            RequiredRole    NVARCHAR(100) NULL,
            RequiredClaimType NVARCHAR(100) NULL,
            RequiredClaimValue NVARCHAR(200) NULL,
            SortOrder       INT           NOT NULL CONSTRAINT DF_UiMode_SortOrder DEFAULT(0),
            IsActive        BIT           NOT NULL CONSTRAINT DF_UiMode_IsActive DEFAULT(1)
        );

        CREATE UNIQUE INDEX UX_UiMode_Code ON dbo.UiMode(Code);
    END;

    IF COL_LENGTH('dbo.UiMode', 'RequiredRole') IS NULL
        ALTER TABLE dbo.UiMode ADD RequiredRole NVARCHAR(100) NULL;

    IF COL_LENGTH('dbo.UiMode', 'RequiredClaimType') IS NULL
        ALTER TABLE dbo.UiMode ADD RequiredClaimType NVARCHAR(100) NULL;

    IF COL_LENGTH('dbo.UiMode', 'RequiredClaimValue') IS NULL
        ALTER TABLE dbo.UiMode ADD RequiredClaimValue NVARCHAR(200) NULL;

    IF OBJECT_ID(N'dbo.UiCategory', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.UiCategory
        (
            UiCategoryId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UiCategory PRIMARY KEY,
            UiModeId             INT           NOT NULL,
            Code                 NVARCHAR(50)  NOT NULL,
            DisplayName          NVARCHAR(100) NOT NULL,
            SortOrder            INT           NOT NULL CONSTRAINT DF_UiCategory_SortOrder DEFAULT(0),
            IsCollapsible        BIT           NOT NULL CONSTRAINT DF_UiCategory_IsCollapsible DEFAULT(1),
            IsExpandedDefault    BIT           NOT NULL CONSTRAINT DF_UiCategory_IsExpandedDefault DEFAULT(1),
            IsActive             BIT           NOT NULL CONSTRAINT DF_UiCategory_IsActive DEFAULT(1),
            CONSTRAINT FK_UiCategory_UiMode FOREIGN KEY (UiModeId) REFERENCES dbo.UiMode(UiModeId)
        );

        CREATE UNIQUE INDEX UX_UiCategory_Mode_Code ON dbo.UiCategory(UiModeId, Code);
    END;

    IF OBJECT_ID(N'dbo.UiNavItem', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.UiNavItem
        (
            UiNavItemId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UiNavItem PRIMARY KEY,
            UiCategoryId     INT            NOT NULL,
            Title            NVARCHAR(120)  NOT NULL,
            IconCss          NVARCHAR(80)   NULL,
            Controller       NVARCHAR(80)   NOT NULL,
            [Action]         NVARCHAR(80)   NOT NULL,
            RouteBereich     NVARCHAR(50)   NULL,
            BadgeText        NVARCHAR(40)   NULL,
            SortOrder        INT            NOT NULL CONSTRAINT DF_UiNavItem_SortOrder DEFAULT(0),
            IsActive         BIT            NOT NULL CONSTRAINT DF_UiNavItem_IsActive DEFAULT(1),
            CONSTRAINT FK_UiNavItem_UiCategory FOREIGN KEY (UiCategoryId) REFERENCES dbo.UiCategory(UiCategoryId)
        );

        CREATE UNIQUE INDEX UX_UiNavItem_Category_Title ON dbo.UiNavItem(UiCategoryId, Title);
    END;

    /* =========================================================
       2) Modi seeden
       ========================================================= */

    MERGE dbo.UiMode AS tgt
    USING (VALUES
        (N'Technik',      N'Technik',      N'Mode.Technik',      NULL, NULL, 10, 1),
        (N'Organisation', N'Organisation', N'Mode.Organisation', NULL, NULL, 20, 1)
    ) AS src(Code, DisplayName, RequiredRole, RequiredClaimType, RequiredClaimValue, SortOrder, IsActive)
    ON tgt.Code = src.Code
    WHEN MATCHED THEN
        UPDATE SET
            DisplayName = src.DisplayName,
            RequiredRole = src.RequiredRole,
            RequiredClaimType = src.RequiredClaimType,
            RequiredClaimValue = src.RequiredClaimValue,
            SortOrder   = src.SortOrder,
            IsActive    = src.IsActive
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (Code, DisplayName, RequiredRole, RequiredClaimType, RequiredClaimValue, SortOrder, IsActive)
        VALUES (src.Code, src.DisplayName, src.RequiredRole, src.RequiredClaimType, src.RequiredClaimValue, src.SortOrder, src.IsActive);

    /* =========================================================
       3) Kategorien seeden
       ========================================================= */

    ;WITH SeedCategories AS
    (
        SELECT
            m.UiModeId,
            s.Code,
            s.DisplayName,
            s.SortOrder,
            s.IsCollapsible,
            s.IsExpandedDefault,
            s.IsActive
        FROM (VALUES
            (N'Technik',      N'General',     N'Allgemein',            10, 1, 1, 1),
            (N'Technik',      N'Insights',    N'Insights',             20, 1, 1, 1),
            (N'Technik',      N'Verbrauch',   N'Verbrauchswerte',      30, 1, 1, 1),
            (N'Technik',      N'Apps',        N'Anwendungen',          40, 1, 1, 1),
            (N'Organisation', N'General',     N'Allgemein',            10, 1, 1, 1),
            (N'Organisation', N'Controlling', N'Management-' + NCHAR(220) + N'bersicht', 20, 1, 1, 1)
        ) s(ModeCode, Code, DisplayName, SortOrder, IsCollapsible, IsExpandedDefault, IsActive)
        INNER JOIN dbo.UiMode m ON m.Code = s.ModeCode
    )
    MERGE dbo.UiCategory AS tgt
    USING SeedCategories AS src
    ON tgt.UiModeId = src.UiModeId AND tgt.Code = src.Code
    WHEN MATCHED THEN
        UPDATE SET
            DisplayName       = src.DisplayName,
            SortOrder         = src.SortOrder,
            IsCollapsible     = src.IsCollapsible,
            IsExpandedDefault = src.IsExpandedDefault,
            IsActive          = src.IsActive
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (UiModeId, Code, DisplayName, SortOrder, IsCollapsible, IsExpandedDefault, IsActive)
        VALUES (src.UiModeId, src.Code, src.DisplayName, src.SortOrder, src.IsCollapsible, src.IsExpandedDefault, src.IsActive);

    /* =========================================================
       4) Navigationseinträge seeden
       ========================================================= */

    ;WITH SeedItems AS
    (
        SELECT
            c.UiCategoryId,
            i.Title,
            i.IconCss,
            i.Controller,
            i.[Action],
            i.RouteBereich,
            i.BadgeText,
            i.SortOrder,
            i.IsActive
        FROM (VALUES
            -- Technik / Allgemein
            (N'Technik',      N'General',     N'Dashboard',            N'ri-dashboard-line', N'Home',      N'Dashboard',         N'Technik',      NULL,   10, 1),
            (N'Technik',      N'General',     N'Leitstand',            N'ri-home-4-line',    N'Home',      N'Index',             N'Technik',      NULL,   20, 1),

            -- Technik / Insights
            (N'Technik',      N'Insights',    N'User Logins',          N'ri-group-line',     N'UserLogins',N'Index',             N'Technik',      N'New', 10, 1),

            -- Technik / Verbrauchswerte
            (N'Technik',      N'Verbrauch',   N'Speicherplatz',        N'ri-server-line',    N'Monitor',   N'Speicherplatz',     N'Technik',      NULL,   10, 1),
            (N'Technik',      N'Verbrauch',   N'FormRec',              N'ri-file-search-line',N'Monitor',  N'FormRecognizer',    N'Technik',      NULL,   20, 1),
            (N'Technik',      N'Verbrauch',   N'M365',                 N'ri-mail-line',      N'Monitor',   N'M365',              N'Technik',      NULL,   30, 1),
            (N'Technik',      N'Verbrauch',   N'Lizenzen',             N'ri-key-2-line',     N'Monitor',   N'Licenses',          N'Technik',      NULL,   40, 1),

            -- Technik / Anwendungen
            (N'Technik',      N'Apps',        N'topfact6 MyWork Cloud',N'ri-cloud-line',     N'Monitor',   N'MyWorkCloud',       N'Technik',      NULL,   10, 1),
            (N'Technik',      N'Apps',        N'topfact6 MyWork',      N'ri-pulse-line',     N'Monitor',   N'topfact6 MyWork',   N'Technik',      NULL,   20, 1),
            (N'Technik',      N'Apps',        N'topfact MyWork App',   N'ri-apps-line',      N'Monitor',   N'topfact MyWork App',N'Technik',      NULL,   30, 1),
            (N'Technik',      N'Apps',        N'topfact6 BestPeople',  N'ri-group-line',     N'Monitor',   N'topfact6 BestPeople',N'Technik',     NULL,   40, 1),
            (N'Technik',      N'Apps',        N'topfact6 Facility',    N'ri-building-4-line',N'Monitor',   N'topfact6 Facility', N'Technik',      NULL,   50, 1),

            -- Organisation / Allgemein
            (N'Organisation', N'General',     N'Dashboard',            N'ri-dashboard-line', N'Home',      N'Dashboard',         N'Organisation', NULL,   10, 1),
            (N'Organisation', N'General',     N'Leitstand',            N'ri-home-4-line',    N'Home',      N'Index',             N'Organisation', NULL,   20, 1),

            -- Organisation / Management-Übersicht
            (N'Organisation', N'Controlling', N'Projekt-Controlling',  N'ri-line-chart-line',N'Monitor',   N'ProjektControlling',N'Organisation', NULL,   10, 1),
            (N'Organisation', N'Controlling', N'Geplante Funktionen',  N'ri-calendar-check-line',N'UserLogins',          N'GeplanteFunktionen', N'Organisation', NULL, 20, 1),
            (N'Organisation', N'Controlling', N'GitHub Controlling',    N'ri-github-line',        N'GitHubControlling',  N'GitHubControlling',  N'Organisation', NULL, 30, 1)
        ) i(ModeCode
        INNER JOIN dbo.UiMode m
            ON m.Code = i.ModeCode
        INNER JOIN dbo.UiCategory c
            ON c.UiModeId = m.UiModeId
           AND c.Code = i.CategoryCode
    )
    MERGE dbo.UiNavItem AS tgt
    USING SeedItems AS src
    ON tgt.UiCategoryId = src.UiCategoryId
       AND tgt.Title = src.Title
    WHEN MATCHED THEN
        UPDATE SET
            IconCss      = src.IconCss,
            Controller   = src.Controller,
            [Action]     = src.[Action],
            RouteBereich = src.RouteBereich,
            BadgeText    = src.BadgeText,
            SortOrder    = src.SortOrder,
            IsActive     = src.IsActive
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (UiCategoryId, Title, IconCss, Controller, [Action], RouteBereich, BadgeText, SortOrder, IsActive)
        VALUES (src.UiCategoryId, src.Title, src.IconCss, src.Controller, src.[Action], src.RouteBereich, src.BadgeText, src.SortOrder, src.IsActive);

    /* Korrektur bereits falsch codierter Werte */
    UPDATE dbo.UiCategory
    SET DisplayName = N'Management-' + NCHAR(220) + N'bersicht'
    WHERE Code = N'Controlling';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

PRINT 'UiMode/UiCategory/UiNavItem erfolgreich erstellt/aktualisiert.';

UPDATE dbo.UiMode
SET RequiredRole = NULL
WHERE Code IN (N'Technik', N'Organisation');
