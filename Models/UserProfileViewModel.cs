namespace topfact.Pulse.Models
{
    public class UserProfileViewModel
    {
        // Aus Claims (immer verfügbar)
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? UserNumber { get; set; }
        public string? Realm { get; set; }
        public string? UserGuid { get; set; }
        public string? OrgGuid { get; set; }
        public int UserId { get; set; }
        public int OrgId { get; set; }

        // Profilbild (Gravatar-URL oder Base64-Data-URL)
        public string? AvatarUrl { get; set; }

        /// <summary>Base64-Profilbild aus der API (data:image/...;base64,...)</summary>
        public string? UserImageBase64 { get; set; }

        /// <summary>Gibt die beste verfügbare Bild-URL zurück: Base64 hat Vorrang vor Gravatar.</summary>
        public string EffectiveAvatarUrl => !string.IsNullOrWhiteSpace(UserImageBase64) ? UserImageBase64 : (AvatarUrl ?? "");

        // Aus API-Abfrage (/api/User)
        public Dictionary<string, string> ApiProperties { get; set; } = new();

        public string? ErrorMessage { get; set; }

        /// <summary>Gravatar-URL aus E-Mail berechnen.</summary>
        public static string GetGravatarUrl(string? email, int size = 80)
        {
            if (string.IsNullOrWhiteSpace(email)) return $"https://www.gravatar.com/avatar/?d=mp&s={size}";
            var hash = System.Security.Cryptography.MD5.HashData(
                System.Text.Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));
            var hex = Convert.ToHexString(hash).ToLowerInvariant();
            return $"https://www.gravatar.com/avatar/{hex}?d=mp&s={size}";
        }
    }
}
