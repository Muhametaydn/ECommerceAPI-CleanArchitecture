using ECommerce.Application.Features.Auth.DTOs;

namespace ECommerce.Application.Contracts.Identity
{
    public interface IAuthService
    {
        Task<RegistrationResponse> RegisterAsync(RegisterRequest request, string ipAddress);
        Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken, string ipAddress);
        Task RevokeTokenAsync(string refreshToken, Guid requesterId);
    }
}
