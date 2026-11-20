using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Profiles.Commands.UpdateProfile;
using ECommerce.Application.Features.Profiles.Commands.ChangePassword;
using ECommerce.Application.Features.Profiles.DTOs;
using ECommerce.Application.Features.Profiles.Queries.GetProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/profile")]
[Authorize(Policy = "Authenticated")]
[EnableRateLimiting("api")]
public sealed class ProfileController : ControllerBase
{
    private const long MaxFileSize = 2 * 1024 * 1024;
    private static readonly Dictionary<string, (string Extension, byte[][] Signatures)> AllowedImages = new()
    {
        ["image/jpeg"] = (".jpg", [[0xFF, 0xD8, 0xFF]]),
        ["image/png"] = (".png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
        ["image/webp"] = (".webp", ["RIFF"u8.ToArray()])
    };

    private readonly IMediator _mediator;
    private readonly IProfileImageStorage _imageStorage;

    public ProfileController(IMediator mediator, IProfileImageStorage imageStorage)
    {
        _mediator = mediator;
        _imageStorage = imageStorage;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetProfileQuery(GetUserId()), cancellationToken));

    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProfileCommand(GetUserId(), request.FirstName, request.LastName, request.ProfileImageUrl);
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(typeof(ProfileImageUploadResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Boş dosya yüklenemez." });
        if (file.Length > MaxFileSize)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ProblemDetails { Title = "Profil görseli en fazla 2 MB olabilir." });
        if (!AllowedImages.TryGetValue(file.ContentType.ToLowerInvariant(), out var imageType))
            return BadRequest(new ProblemDetails { Title = "Yalnızca JPEG, PNG veya WebP görselleri yüklenebilir." });

        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await input.ReadAsync(header, cancellationToken);
        input.Position = 0;
        var valid = imageType.Signatures.Any(signature => bytesRead >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature));
        if (file.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            valid = bytesRead >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        if (!valid)
            return BadRequest(new ProblemDetails { Title = "Dosya içeriği geçerli bir görsel değil." });

        var url = await _imageStorage.SaveAsync(input, imageType.Extension, cancellationToken);
        return Created(url, new ProfileImageUploadResponse(url));
    }

    [HttpPut("password")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ChangePasswordCommand(GetUserId(), request.CurrentPassword, request.NewPassword), cancellationToken);
        return NoContent();
    }

    [HttpPost("email-change")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestEmailChange(RequestEmailChangeRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ECommerce.Application.Features.Profiles.Commands.RequestEmailChange.RequestEmailChangeCommand(GetUserId(), request.NewEmail, request.CurrentPassword), cancellationToken);
        return Accepted(new { message = "Adres uygunsa doğrulama bağlantısı yeni e-posta adresine gönderildi." });
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var userId) ? userId : throw new UnauthorizedAccessException("Geçersiz kullanıcı bilgisi.");
    }
}

public sealed record UpdateProfileRequest(string FirstName, string LastName, string? ProfileImageUrl);
public sealed record ProfileImageUploadResponse(string Url);
public sealed record RequestEmailChangeRequest(string NewEmail, string CurrentPassword);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
