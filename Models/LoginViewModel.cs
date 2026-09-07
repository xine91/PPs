using System.ComponentModel.DataAnnotations;

namespace topfact.Pulse.Models;
public class LoginViewModel
{
    [Required(ErrorMessage = "Benutzername ist erforderlich")]
    [Display(Name = "Benutzername")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Kennwort ist erforderlich")]
    [DataType(DataType.Password)]
    [Display(Name = "Kennwort")]
    public required string Password { get; set; }

    [Display(Name = "Angemeldet bleiben")]
    public bool RememberMe { get; set; } = false;
}