using System.Text;
using System.Security.Cryptography;

namespace topfact.Pulse.Models
{
    public class LoginViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? RedirectUrl { get; set; }
    }

    public class UserProfileViewModel
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? UserNumber { get; set; }
        public string? Realm { get; set; }
        public string? UserGuid { get; set; }
        public string? OrgGuid { get; set; }
        public int UserId { get; set; }
        public int OrgId { get; set; }
        public string? AvatarUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, string> ApiProperties { get; set; } = new();

        public string? EffectiveAvatarUrl => AvatarUrl ?? GetGravatarUrl(Email);

        public static string GetGravatarUrl(string? email, int size = 80)
        {
            if (string.IsNullOrWhiteSpace(email))
                return $"https://www.gravatar.com/avatar/?d=identicon&s={size}";

            var hash = GetMd5Hash(email.Trim().ToLowerInvariant());
            return $"https://www.gravatar.com/avatar/{hash}?d=identicon&s={size}";
        }

        private static string GetMd5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
