namespace topfact.Pulse.Models
{
    /// <summary>Response DTO für einzelne KPI-Definition</summary>
    public class KpiResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string IconCss { get; set; } = "ri-bar-chart-line";
        public string Color { get; set; } = "#338562";
        public string? Bereich { get; set; }
        public string? QuerySql { get; set; }
        public double? TargetValue { get; set; }
        public double? ToleranceAbsolute { get; set; }
        public double? TolerancePercent { get; set; }
        public double? ThresholdGreen { get; set; }
        public double? ThresholdYellow { get; set; }
        public string? Unit { get; set; }
        public string? DisplayStyle { get; set; }
        public int SortOrder { get; set; }
        public int? KategorieID { get; set; }
        public int? ConnectorID { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>Request DTO für KPI speichern (Create/Update)</summary>
    public class KpiSaveDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string IconCss { get; set; } = "ri-bar-chart-line";
        public string Color { get; set; } = "#338562";
        public string? Bereich { get; set; }
        public string? QuerySql { get; set; }
        public double? TargetValue { get; set; }
        public double? ToleranceAbsolute { get; set; }
        public double? TolerancePercent { get; set; }
        public double? ThresholdGreen { get; set; }
        public double? ThresholdYellow { get; set; }
        public string? Unit { get; set; }
        public string? DisplayStyle { get; set; }
        public int SortOrder { get; set; }
        public int? KategorieID { get; set; }
        public int? ConnectorID { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>Response DTO für Datenquellen (Connectors)</summary>
    public class DataSourceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Connector";
        public int? KategorieID { get; set; }
    }
}
