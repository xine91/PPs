using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    [Table("Pulse_VorgangSnapshot", Schema = "dbo")]
    public class VorgangSnapshot
    {
        [Key]
        public int Id { get; set; }

        public DateTime SnapshotTimeUtc { get; set; }

        public int VorgangsNr { get; set; }

        [MaxLength(250)]
        public string? Firma { get; set; }

        [MaxLength(250)]
        public string? Vorgangstitel { get; set; }

        [MaxLength(250)]
        public string? Vorgangsart { get; set; }

        [MaxLength(250)]
        public string? Artikel { get; set; }

        [MaxLength(250)]
        public string? StatusName { get; set; }

        [MaxLength(250)]
        public string? StatusColor { get; set; }

        [MaxLength(250)]
        public string? Bearbeiter { get; set; }

        public DateTime? Erfassungsdatum { get; set; }

        public DateTime? DateModified { get; set; }

        public int DauerIstMinuten { get; set; }

        public int AktionenOffen { get; set; }

        public int AktionenInBearbeitung { get; set; }

        public int AktionenErledigt { get; set; }

        public int AnzahlKommentare { get; set; }

        public int KundeWartet { get; set; }
    }
}
