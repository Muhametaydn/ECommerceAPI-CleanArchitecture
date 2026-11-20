namespace ECommerce.Infrastructure.Settings
{
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        public string SecretKey { get; set; } = default!;
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public int AccessTokenExpirationMinutes { get; set; } = 15;
        public int RefreshTokenExpirationDays { get; set; } = 7;

        /// <summary>HMAC-SHA256 için en az 256 bit (32 bayt) anahtar gerekir.</summary>
        public const int MinimumSecretKeyBytes = 32;

        /// <summary>
        /// Boş, kısa veya repoda yayınlanmış örnek anahtarlarla uygulamanın başlamasını engeller.
        /// Bu anahtarı bilen herkes Admin rolünde geçerli JWT üretebilir.
        /// </summary>
        public void EnsureSecretKeyIsSafe()
        {
            if (string.IsNullOrWhiteSpace(SecretKey))
                throw new InvalidOperationException(
                    "JwtSettings:SecretKey tanımlı değil. JwtSettings__SecretKey ortam değişkeniyle güçlü bir anahtar verin.");

            if (System.Text.Encoding.UTF8.GetByteCount(SecretKey) < MinimumSecretKeyBytes)
                throw new InvalidOperationException(
                    $"JwtSettings:SecretKey en az {MinimumSecretKeyBytes} bayt olmalıdır.");

            if (SecretKey.Contains("CHANGE-THIS", StringComparison.OrdinalIgnoreCase)
                || SecretKey.Contains("change-me", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "JwtSettings:SecretKey örnek (placeholder) değerde bırakılmış. Gerçek, gizli bir anahtar verin.");
        }
    }
}
