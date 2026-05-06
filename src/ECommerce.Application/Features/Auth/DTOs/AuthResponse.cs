namespace ECommerce.Application.Features.Auth.DTOs
{
    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public IList<string> Roles { get; set; } = new List<string>();
        public string AccessToken { get; set; } = default!;
        public DateTime AccessTokenExpires { get; set; }
        public string RefreshToken { get; set; } = default!;
        public DateTime RefreshTokenExpires { get; set; }
    }
}
