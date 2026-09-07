using System.ComponentModel.DataAnnotations;

public class ClientStartupInformation
{
    [Key] // Markiert die folgende Spalte als Primärschlüssel
    public int InfoID { get; set; }

    // Das '?' ist entscheidend, damit EF mit NULL-Werten aus der DB umgehen kann
    public string? Customer { get; set; }

    public string? AppName { get; set; }

    public string? Clientname { get; set; }

    public string? Username { get; set; }

    public string? Domainname { get; set; }

    public string? AppVersion { get; set; }

    // Falls das Datum in der DB leer sein kann:
    public DateTime? DateCreated { get; set; }
}