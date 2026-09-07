using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace topfact.Pulse.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Pulse_Bereich",
                schema: "dbo",
                columns: table => new
                {
                    BereichID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequiredRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pulse_Bereich", x => x.BereichID);
                });

            migrationBuilder.CreateTable(
                name: "Pulse_ConnectorManager",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    connector_type = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    api_config = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sql_config = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    manual_config = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bezeichnung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KategorieID = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pulse_ConnectorManager", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pulse_Logins",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    login_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    success = table.Column<bool>(type: "bit", nullable: false),
                    ip_address = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pulse_Logins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pulse_Gruppe",
                schema: "dbo",
                columns: table => new
                {
                    GruppeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BereichID = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCollapsible = table.Column<bool>(type: "bit", nullable: false),
                    IsExpandedDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pulse_Gruppe", x => x.GruppeID);
                    table.ForeignKey(
                        name: "FK_Pulse_Gruppe_Pulse_Bereich_BereichID",
                        column: x => x.BereichID,
                        principalSchema: "dbo",
                        principalTable: "Pulse_Bereich",
                        principalColumn: "BereichID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pulse_Access",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BereichID = table.Column<int>(type: "int", nullable: true),
                    GruppeID = table.Column<int>(type: "int", nullable: true),
                    Permission = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "View"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pulse_Access", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pulse_Access_Pulse_Bereich_BereichID",
                        column: x => x.BereichID,
                        principalSchema: "dbo",
                        principalTable: "Pulse_Bereich",
                        principalColumn: "BereichID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pulse_Access_Pulse_Gruppe_GruppeID",
                        column: x => x.GruppeID,
                        principalSchema: "dbo",
                        principalTable: "Pulse_Gruppe",
                        principalColumn: "GruppeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pulse_Kategorie",
                schema: "dbo",
                columns: table => new
                {
                    KategorieID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GruppeID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IconCss = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RouteBereich = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BadgeText = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Sql_query = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConnectorID = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pulse_Kategorie", x => x.KategorieID);
                    table.ForeignKey(
                        name: "FK_Pulse_Kategorie_Pulse_Gruppe_GruppeID",
                        column: x => x.GruppeID,
                        principalSchema: "dbo",
                        principalTable: "Pulse_Gruppe",
                        principalColumn: "GruppeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pulse_KpiDefinition",
                schema: "dbo",
                columns: table => new
                {
                    KpiDefinitionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IconCss = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, defaultValue: "ri-bar-chart-line"),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "#338562"),
                    Bereich = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QuerySql = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TargetValue = table.Column<double>(type: "float", nullable: true),
                    ToleranceAbsolute = table.Column<double>(type: "float", nullable: true),
                    TolerancePercent = table.Column<double>(type: "float", nullable: true),
                    ThresholdGreen = table.Column<double>(type: "float", nullable: true),
                    ThresholdYellow = table.Column<double>(type: "float", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DisplayStyle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    KategorieID = table.Column<int>(type: "int", nullable: true),
                    ConnectorID = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pulse_KpiDefinition", x => x.KpiDefinitionID);
                    table.ForeignKey(
                        name: "FK_Pulse_KpiDefinition_Pulse_Kategorie_KategorieID",
                        column: x => x.KategorieID,
                        principalSchema: "dbo",
                        principalTable: "Pulse_Kategorie",
                        principalColumn: "KategorieID");
                });

            migrationBuilder.CreateTable(
                name: "Pulse_QueryResults",
                schema: "dbo",
                columns: table => new
                {
                    QueryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KategorieID = table.Column<int>(type: "int", nullable: true),
                    Sql_query = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Daten = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pulse_QueryResults", x => x.QueryID);
                    table.ForeignKey(
                        name: "FK_Pulse_QueryResults_Pulse_Kategorie_KategorieID",
                        column: x => x.KategorieID,
                        principalSchema: "dbo",
                        principalTable: "Pulse_Kategorie",
                        principalColumn: "KategorieID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_Access_BereichID_GruppeID",
                schema: "dbo",
                table: "Pulse_Access",
                columns: new[] { "BereichID", "GruppeID" });

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_Access_GruppeID",
                schema: "dbo",
                table: "Pulse_Access",
                column: "GruppeID");

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_Access_user_name_IsActive",
                schema: "dbo",
                table: "Pulse_Access",
                columns: new[] { "user_name", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_Bereich_Code",
                schema: "dbo",
                table: "Pulse_Bereich",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_Bereich_IsActive",
                schema: "dbo",
                table: "Pulse_Bereich",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_Gruppe_BereichID_Code",
                schema: "dbo",
                table: "Pulse_Gruppe",
                columns: new[] { "BereichID", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_Gruppe_IsActive",
                schema: "dbo",
                table: "Pulse_Gruppe",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_Kategorie_GruppeID_Title",
                schema: "dbo",
                table: "Pulse_Kategorie",
                columns: new[] { "GruppeID", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_Kategorie_IsActive",
                schema: "dbo",
                table: "Pulse_Kategorie",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_KpiDefinition_Bereich",
                schema: "dbo",
                table: "Pulse_KpiDefinition",
                column: "Bereich");

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_KpiDefinition_IsActive",
                schema: "dbo",
                table: "Pulse_KpiDefinition",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_KpiDefinition_KategorieID",
                schema: "dbo",
                table: "Pulse_KpiDefinition",
                column: "KategorieID");

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_Logins_success",
                schema: "dbo",
                table: "Pulse_Logins",
                column: "success");

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_Logins_user_name_login_time",
                schema: "dbo",
                table: "Pulse_Logins",
                columns: new[] { "user_name", "login_time" });

            migrationBuilder.CreateIndex(
                name: "IX_Pulse_QueryResults_KategorieID",
                schema: "dbo",
                table: "Pulse_QueryResults",
                column: "KategorieID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pulse_Access",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Pulse_ConnectorManager",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Pulse_KpiDefinition",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Pulse_Logins",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Pulse_QueryResults",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Pulse_Kategorie",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Pulse_Gruppe",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Pulse_Bereich",
                schema: "dbo");
        }
    }
}
