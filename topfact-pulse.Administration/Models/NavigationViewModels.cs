namespace topfact.Pulse.Web.Controllers
{
    // Delete Models nur für die Admin-API
    public class DeleteBereichModel
    {
        public int bereichId { get; set; }
    }

    public class DeleteGruppeModel
    {
        public int gruppeId { get; set; }
    }

    public class DeleteKategorieModel
    {
        public int kategorieId { get; set; }
    }
}
