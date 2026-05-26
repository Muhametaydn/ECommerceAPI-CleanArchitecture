namespace ECommerce.Infrastructure.Settings
{
    public sealed class EmailSettings
    {
        public const string SectionName = "EmailSettings";

        public string From { get; set; } = "noreply@ecommerce.com";
        public string FromName { get; set; } = "ECommerce";
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 1025;
        public bool EnableSsl { get; set; } = false;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
