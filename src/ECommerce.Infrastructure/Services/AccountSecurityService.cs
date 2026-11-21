using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ECommerce.Infrastructure.Settings;
using ECommerce.Application.Common.Fraud;
using ECommerce.Contracts.Fraud;
using ECommerce.Domain.Outbox;
using System.Text.Json;

namespace ECommerce.Infrastructure.Services;

public sealed class AccountSecurityService : IAccountSecurityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;
    private readonly FraudDetectionSettings _fraud;

    public AccountSecurityService(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IEmailService emailService, IOptions<EmailSettings> emailSettings,
        FraudDetectionSettings? fraud = null)
    {
        _fraud = fraud ?? new FraudDetectionSettings();
        _userManager = userManager;
        _dbContext = dbContext;
        _emailService = emailService;
        _emailSettings = emailSettings.Value;
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code == "PasswordMismatch"))
                throw new InvalidOperationException("Mevcut şifre hatalı.");

            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        var activeTokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
            token.Revoke();

        QueueSecurityChange(userId, SecurityChangeKinds.PasswordChanged);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
            return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = $"{_emailSettings.FrontendBaseUrl.TrimEnd('/')}/sifre-sifirla?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendPasswordResetAsync(user.Email, user.FullName, resetUrl, cancellationToken);
    }

    public async Task ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive)
            throw new InvalidOperationException("Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException("Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.");

        var activeTokens = await _dbContext.RefreshTokens
            .Where(refreshToken => refreshToken.UserId == user.Id && refreshToken.RevokedAt == null && refreshToken.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        foreach (var refreshToken in activeTokens)
            refreshToken.Revoke();

        QueueSecurityChange(user.Id, SecurityChangeKinds.PasswordReset);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RequestEmailConfirmationAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive || user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
            return;

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = $"{_emailSettings.FrontendBaseUrl.TrimEnd('/')}/eposta-dogrula?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendEmailConfirmationAsync(user.Email, user.FullName, confirmationUrl, cancellationToken);
    }

    public async Task RequestEmailChangeAsync(Guid userId, string newEmail, string currentPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Geçersiz kullanıcı bilgisi.");
        if (!await _userManager.CheckPasswordAsync(user, currentPassword))
            throw new InvalidOperationException("Mevcut şifre hatalı.");

        newEmail = newEmail.Trim();
        // Başka bir hesabın varlığını açıklama; mevcut adresi doğrulamadan değiştirme.
        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase)
            || await _userManager.FindByEmailAsync(newEmail) is not null)
            return;

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var url = $"{_emailSettings.FrontendBaseUrl.TrimEnd('/')}/eposta-degistir?userId={user.Id}&newEmail={Uri.EscapeDataString(newEmail)}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendEmailChangeConfirmationAsync(newEmail, user.FullName, url, cancellationToken);
    }

    public async Task ConfirmEmailChangeAsync(Guid userId, string newEmail, string token, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            throw new InvalidOperationException("E-posta değiştirme bağlantısı geçersiz veya süresi dolmuş.");
        newEmail = newEmail.Trim();

        if (!await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.ChangeEmailTokenProvider, "ChangeEmail:" + newEmail, token))
            throw new InvalidOperationException("E-posta değiştirme bağlantısı geçersiz veya süresi dolmuş.");

        // E-posta benzersizliği kontrolü ve güncelleme aynı işlemde tamamlanır.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase)
            || await _userManager.FindByEmailAsync(newEmail) is not null)
            throw new InvalidOperationException("E-posta değiştirme bağlantısı geçersiz veya adres kullanılamıyor.");

        var oldEmail = user.Email;
        var result = await _userManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
            throw new InvalidOperationException("E-posta değiştirme bağlantısı geçersiz veya süresi dolmuş.");

        if (string.Equals(user.UserName, oldEmail, StringComparison.OrdinalIgnoreCase))
        {
            var usernameResult = await _userManager.SetUserNameAsync(user, newEmail);
            if (!usernameResult.Succeeded)
                throw new InvalidOperationException("Yeni e-posta adresi kullanılamıyor.");
        }

        var activeTokens = await _dbContext.RefreshTokens
            .Where(refresh => refresh.UserId == userId && refresh.RevokedAt == null && refresh.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
        foreach (var refresh in activeTokens)
            refresh.Revoke();
        QueueSecurityChange(userId, SecurityChangeKinds.EmailChanged);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive)
            throw new InvalidOperationException("E-posta doğrulama bağlantısı geçersiz veya süresi dolmuş.");
        if (user.EmailConfirmed)
            return;

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            throw new InvalidOperationException("E-posta doğrulama bağlantısı geçersiz veya süresi dolmuş.");
    }

    /// <summary>
    /// Fraud açıksa hesap güvenliği değişikliğini aynı transaction'da outbox'a yazar (hesap ele geçirme sinyali).
    /// Değişiklik geri alınırsa olay da yazılmaz. Yeni e-posta veya şifre bilgisi gönderilmez.
    /// </summary>
    private void QueueSecurityChange(Guid userId, string changeKind)
    {
        if (!_fraud.Enabled)
            return;

        var message = new AccountSecurityChanged
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            UserId = userId,
            ChangeKind = changeKind
        };
        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(AccountSecurityChanged).FullName!,
            Payload = JsonSerializer.Serialize(message),
            CreatedAt = DateTime.UtcNow
        });
    }
}
