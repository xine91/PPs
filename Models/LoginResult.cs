namespace topfact.Pulse.Models;

public class LoginResult
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public string? Token { get; set; }

    public string? RedirectUrl { get; set; }  

    public UserInfo User { get; set; } = new UserInfo();
}
