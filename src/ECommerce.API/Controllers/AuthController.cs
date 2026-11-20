using System.Security.Claims;
using Asp.Versioning;
using System.Text.Json.Serialization;
using ECommerce.Application.Contracts.Identity;
using ECommerce.Application.Features.Auth.DTOs;
using ECommerce.Application.Features.Auth.Commands.ForgotPassword;
using ECommerce.Application.Features.Auth.Commands.ResetPassword;
using ECommerce.Application.Features.Auth.Commands.ConfirmEmail;
using ECommerce.Application.Features.Auth.Commands.RequestEmailConfirmation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IMediator _mediator;

        public AuthController(IAuthService authService, IMediator mediator)
        {
            _authService = authService;
            _mediator = mediator;
        }

        /// <summary>
        /// Yeni kullanıcı kaydı oluşturur (Customer rolüyle)
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.RegisterAsync(request, ipAddress);
            return CreatedAtAction(nameof(Register), response);
        }

        /// <summary>
        /// Kullanıcı girişi yapar ve JWT + Refresh Token döner
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.LoginAsync(request, ipAddress);
            return Ok(response);
        }

        /// <summary>
        /// Refresh token ile yeni access token alır
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);
            return Ok(response);
        }

        /// <summary>
        /// Refresh token'ı geçersiz kılar (logout)
        /// </summary>
        [HttpPost("revoke-token")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _authService.RevokeTokenAsync(request.RefreshToken, userId);
            return NoContent();
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
            return Accepted(new { message = "Hesap mevcutsa şifre sıfırlama bağlantısı e-posta adresine gönderildi." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword), cancellationToken);
            return NoContent();
        }

        [HttpPost("request-email-confirmation")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> RequestEmailConfirmation([FromBody] EmailConfirmationRequest request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new RequestEmailConfirmationCommand(request.Email), cancellationToken);
            return Accepted(new { message = "Hesap uygunsa doğrulama bağlantısı e-posta adresine gönderildi." });
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new ConfirmEmailCommand(request.Email, request.Token), cancellationToken);
            return NoContent();
        }

        [HttpPost("confirm-email-change")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ConfirmEmailChange(ConfirmEmailChangeRequest request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new ECommerce.Application.Features.Auth.Commands.ConfirmEmailChange.ConfirmEmailChangeCommand(request.UserId, request.NewEmail, request.Token), cancellationToken);
            return NoContent();
        }

        // ── Yardımcı: İstemci IP adresini al ─────────────────────────────
        private string GetIpAddress()
        {
            // Forwarded headers must only be processed by middleware configured with trusted proxies.
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
    public sealed record EmailConfirmationRequest(string Email);
    public sealed record ConfirmEmailChangeRequest([property: JsonRequired] Guid UserId, string NewEmail, string Token);
    public sealed record ConfirmEmailRequest(string Email, string Token);
}
