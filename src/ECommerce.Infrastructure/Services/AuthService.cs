using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Contracts.Identity;
using ECommerce.Application.Features.Auth.DTOs;
using ECommerce.Domain.Constants;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Settings;
using ECommerce.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOptions<JwtSettings> jwtSettings)
        {
            _userManager = userManager;
            _context = context;
            _jwtSettings = jwtSettings.Value;
        }

        // ── REGISTER ──────────────────────────────────────────────────────
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress)
        {
            // E-posta kontrolü
            if (await _userManager.FindByEmailAsync(request.Email) is not null)
                throw new InvalidOperationException($"'{request.Email}' e-posta adresi zaten kayıtlı.");

            var user = ApplicationUser.Create(
                request.FirstName,
                request.LastName,
                request.Email,
                request.UserName);

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Kullanıcı oluşturulamadı: {errors}");
            }

            // Varsayılan olarak Customer rolü ata
            await _userManager.AddToRoleAsync(user, AppRoles.Customer);

            return await BuildAuthResponseAsync(user, ipAddress);
        }

        // ── LOGIN ─────────────────────────────────────────────────────────
        public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress)
        {
            var user = await _userManager.FindByEmailAsync(request.Email)
                ?? throw new UnauthorizedAccessException("E-posta veya şifre hatalı.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Hesabınız devre dışı bırakılmış.");

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
                throw new UnauthorizedAccessException("E-posta veya şifre hatalı.");

            // Eski aktif refresh token'ları iptal et
            var oldTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var old in oldTokens)
                old.Revoke();

            return await BuildAuthResponseAsync(user, ipAddress);
        }

        // ── REFRESH TOKEN ─────────────────────────────────────────────────
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string ipAddress)
        {
            var token = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken)
                ?? throw new UnauthorizedAccessException("Geçersiz refresh token.");

            if (!token.IsActive)
                throw new UnauthorizedAccessException("Refresh token süresi dolmuş veya iptal edilmiş.");

            var user = token.User;

            // Yeni refresh token üret ve eskisini iptal et
            var newRefreshToken = GenerateRefreshToken(user.Id, ipAddress);
            token.Revoke(newRefreshToken.Token);

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = GenerateAccessToken(user, roles);

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Roles = roles,
                AccessToken = accessToken,
                AccessTokenExpires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpires = newRefreshToken.ExpiresAt
            };
        }

        // ── REVOKE ────────────────────────────────────────────────────────
        public async Task RevokeTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken)
                ?? throw new UnauthorizedAccessException("Geçersiz refresh token.");

            if (!token.IsActive)
                throw new InvalidOperationException("Token zaten iptal edilmiş veya süresi dolmuş.");

            token.Revoke();
            await _context.SaveChangesAsync();
        }

        // ── YARDIMCI METODLAR ─────────────────────────────────────────────
        private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user, string ipAddress)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = GenerateAccessToken(user, roles);
            var refreshToken = GenerateRefreshToken(user.Id, ipAddress);

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Roles = roles,
                AccessToken = accessToken,
                AccessTokenExpires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                RefreshToken = refreshToken.Token,
                RefreshTokenExpires = refreshToken.ExpiresAt
            };
        }

        private string GenerateAccessToken(ApplicationUser user, IList<string> roles)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email!),
                new(JwtRegisteredClaimNames.GivenName, user.FirstName),
                new(JwtRegisteredClaimNames.FamilyName, user.LastName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            // Rol claim'leri ekle
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(Guid userId, string ipAddress)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            return RefreshToken.Create(userId, token, _jwtSettings.RefreshTokenExpirationDays, ipAddress);
        }
    }
}
