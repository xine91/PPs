namespace topfact.Pulse.Web.Controllers
{
    public class ExecuteSqlQueryRequest
    {
        public string? SqlQuery { get; set; }
        public int? KategorieID { get; set; }
        public int? ConnectorID { get; set; }
    }
}
