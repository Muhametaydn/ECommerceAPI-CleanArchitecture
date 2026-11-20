using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Auth.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Services;
using ECommerce.Infrastructure.Settings;
using ECommerce.Persistence.Context;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ECommerce.IntegrationTests;

// Real Identity hashing/token providers and relational transactions; no SMTP or live customer data.
public sealed class AccountSecurityPersistenceTests : IAsyncLifetime
{
    private const string Password = "Original123!";
    private const string NewPassword = "Changed456!";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly Mock<IEmailService> _email = new();
    private ServiceProvider _services = null!;
    private Guid _userId, _otherId;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        services.AddSingleton(_email.Object);
        services.Configure<EmailSettings>(options => options.FrontendBaseUrl = "https://shop.example.invalid/");
        services.Configure<JwtSettings>(options => { options.SecretKey = "TEST-ONLY-SECRET-AT-LEAST-32-CHARACTERS-LONG"; options.Issuer = "test"; options.Audience = "test"; options.RefreshTokenExpirationDays = 7; options.AccessTokenExpirationMinutes = 15; });
        services.AddScoped<IAccountSecurityService, AccountSecurityService>();
        services.AddScoped<AuthService>();
        _services = services.BuildServiceProvider();
        await Run(async scope =>
        {
            var db = scope.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();
            var manager = scope.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var address in new[] { "first@example.invalid", "other@example.invalid" })
            {
                var user = ApplicationUser.Create("Test", "Customer", address, address);
                user.EmailConfirmed = true;
                Assert.True((await manager.CreateAsync(user, Password)).Succeeded);
                if (_userId == Guid.Empty) _userId = user.Id; else _otherId = user.Id;
                db.RefreshTokens.Add(RefreshToken.Create(user.Id, "refresh-" + user.Id, 7, "127.0.0.1"));
            }
            await db.SaveChangesAsync();
        });
    }

    private async Task Run(Func<IServiceProvider, Task> action)
    { using var scope = _services.CreateScope(); await action(scope.ServiceProvider); }
    private Task Security(Func<IAccountSecurityService, Task> action)
        => Run(scope => action(scope.GetRequiredService<IAccountSecurityService>()));
    private async Task<string> Token(bool reset = true)
    {
        string token = "";
        await Run(async scope => { var manager = scope.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await manager.FindByIdAsync(_userId.ToString()))!;
            token = reset ? await manager.GeneratePasswordResetTokenAsync(user) : await manager.GenerateEmailConfirmationTokenAsync(user); });
        return token;
    }
    private Task CheckPasswordAndSessions(string password, bool revoked)
        => Run(async scope => { var manager = scope.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True(await manager.CheckPasswordAsync((await manager.FindByIdAsync(_userId.ToString()))!, password));
            var db = scope.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(revoked, (await db.RefreshTokens.SingleAsync(token => token.UserId == _userId)).IsRevoked);
            Assert.False((await db.RefreshTokens.SingleAsync(token => token.UserId == _otherId)).IsRevoked); });

    [Fact]
    public async Task Password_change_revokes_only_owners_sessions_and_rejects_old_password()
    {
        await Security(service => service.ChangePasswordAsync(_userId, Password, NewPassword));
        await CheckPasswordAndSessions(NewPassword, true);
        await Run(async scope => Assert.False(await scope.GetRequiredService<UserManager<ApplicationUser>>().CheckPasswordAsync((await scope.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(_userId.ToString()))!, Password)));
    }

    [Theory]
    [InlineData("Wrong123!", NewPassword)]
    [InlineData(Password, "weak")]
    public async Task Rejected_password_change_preserves_password_and_sessions(string current, string next)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Security(service => service.ChangePasswordAsync(_userId, current, next)));
        await CheckPasswordAndSessions(Password, false);
    }

    [Fact]
    public async Task Password_change_rolls_back_if_session_revocation_cannot_be_saved()
    {
        await Run(scope => scope.GetRequiredService<ApplicationDbContext>().Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_revocation BEFORE UPDATE ON RefreshTokens BEGIN SELECT RAISE(ABORT, 'test failure'); END;"));
        await Assert.ThrowsAsync<DbUpdateException>(() => Security(service => service.ChangePasswordAsync(_userId, Password, NewPassword)));
        await CheckPasswordAndSessions(Password, false);
    }

    [Fact]
    public async Task Reset_token_is_single_use_and_revokes_only_owners_sessions()
    {
        var token = await Token();
        await Security(service => service.ResetPasswordAsync("first@example.invalid", token, NewPassword));
        await CheckPasswordAndSessions(NewPassword, true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Security(service => service.ResetPasswordAsync("first@example.invalid", token, "Another789!")));
        await CheckPasswordAndSessions(NewPassword, true);
    }

    [Theory]
    [InlineData("other@example.invalid")]
    [InlineData("missing@example.invalid")]
    public async Task Reset_token_cannot_be_used_for_another_account(string address)
    {
        var token = await Token();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Security(service => service.ResetPasswordAsync(address, token, NewPassword)));
        await CheckPasswordAndSessions(Password, false);
    }

    [Fact]
    public async Task Invalid_reset_token_preserves_password_and_sessions()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Security(service => service.ResetPasswordAsync("first@example.invalid", "corrupted", NewPassword)));
        await CheckPasswordAndSessions(Password, false);
    }

    [Fact]
    public async Task Reset_rolls_back_password_if_session_revocation_fails()
    {
        var token = await Token();
        await Run(scope => scope.GetRequiredService<ApplicationDbContext>().Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_revocation BEFORE UPDATE ON RefreshTokens BEGIN SELECT RAISE(ABORT, 'test failure'); END;"));
        await Assert.ThrowsAsync<DbUpdateException>(() => Security(service => service.ResetPasswordAsync("first@example.invalid", token, NewPassword)));
        await CheckPasswordAndSessions(Password, false);
    }

    [Fact]
    public async Task Reset_request_sends_encoded_valid_token_only_to_active_known_user()
    {
        string? url = null;
        _email.Setup(service => service.SendPasswordResetAsync("first@example.invalid", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, value, _) => url = value).Returns(Task.CompletedTask);
        await Security(service => service.RequestPasswordResetAsync(" first@example.invalid "));
        Assert.StartsWith("https://shop.example.invalid/sifre-sifirla?", url);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(url!).Query);
        await Security(service => service.ResetPasswordAsync(query["email"].ToString(), query["token"].ToString(), NewPassword));
        await CheckPasswordAndSessions(NewPassword, true);
        await Security(service => service.RequestPasswordResetAsync("missing@example.invalid"));
        await Run(async scope => { var db = scope.GetRequiredService<ApplicationDbContext>(); var user = await db.Set<ApplicationUser>().FindAsync(_userId); user!.Deactivate(); await db.SaveChangesAsync(); });
        await Security(service => service.RequestPasswordResetAsync("first@example.invalid"));
        _email.Verify(service => service.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Email_confirmation_is_bound_to_user_and_rejects_corruption()
    {
        await Run(async scope => { var db = scope.GetRequiredService<ApplicationDbContext>(); (await db.Set<ApplicationUser>().FindAsync(_userId))!.EmailConfirmed = false; (await db.Set<ApplicationUser>().FindAsync(_otherId))!.EmailConfirmed = false; await db.SaveChangesAsync(); });
        var token = await Token(false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Security(service => service.ConfirmEmailAsync("other@example.invalid", token)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Security(service => service.ConfirmEmailAsync("first@example.invalid", "corrupted")));
        await Security(service => service.ConfirmEmailAsync("first@example.invalid", token));
        await Run(async scope => { var db = scope.GetRequiredService<ApplicationDbContext>(); Assert.True((await db.Set<ApplicationUser>().FindAsync(_userId))!.EmailConfirmed); Assert.False((await db.Set<ApplicationUser>().FindAsync(_otherId))!.EmailConfirmed); });
    }

    [Fact]
    public async Task Email_change_requires_password_and_token_for_exact_address()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Security(service => service.RequestEmailChangeAsync(_userId, "new@example.invalid", "Wrong123!")));
        _email.Verify(service => service.SendEmailChangeConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        string token = "";
        await Run(async scope => { var manager = scope.GetRequiredService<UserManager<ApplicationUser>>(); token = await manager.GenerateChangeEmailTokenAsync((await manager.FindByIdAsync(_userId.ToString()))!, "new@example.invalid"); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => Security(service => service.ConfirmEmailChangeAsync(_userId, "wrong@example.invalid", token)));
        await Security(service => service.ConfirmEmailChangeAsync(_userId, "new@example.invalid", token));
        await Run(async scope => { var db = scope.GetRequiredService<ApplicationDbContext>(); var user = (await db.Set<ApplicationUser>().FindAsync(_userId))!; Assert.Equal("new@example.invalid", user.Email); Assert.Equal(user.Email, user.UserName); Assert.True(user.EmailConfirmed); });
        await CheckPasswordAndSessions(Password, true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Security(service => service.ConfirmEmailChangeAsync(_userId, "new@example.invalid", token)));
    }

    [Fact]
    public async Task Refresh_rotation_rejects_reuse_without_creating_extra_tokens()
    {
        await Run(async scope => { var auth = scope.GetRequiredService<AuthService>(); var old = "refresh-" + _userId;
            var response = await auth.RefreshTokenAsync(old, "127.0.0.2");
            Assert.NotEqual(old, response.RefreshToken);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.RefreshTokenAsync(old, "127.0.0.2"));
            var db = scope.GetRequiredService<ApplicationDbContext>(); Assert.Equal(2, await db.RefreshTokens.CountAsync(token => token.UserId == _userId));
            Assert.Equal(response.RefreshToken, (await db.RefreshTokens.SingleAsync(token => token.Token == old)).ReplacedByToken); });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Inactive_or_unconfirmed_user_cannot_refresh(bool deactivate)
    {
        await Run(async scope => { var db = scope.GetRequiredService<ApplicationDbContext>(); var user = (await db.Set<ApplicationUser>().FindAsync(_userId))!; if (deactivate) user.Deactivate(); else user.EmailConfirmed = false; await db.SaveChangesAsync(); });
        await Run(async scope => { await Assert.ThrowsAsync<UnauthorizedAccessException>(() => scope.GetRequiredService<AuthService>().RefreshTokenAsync("refresh-" + _userId, "127.0.0.1"));
            Assert.Equal(1, await scope.GetRequiredService<ApplicationDbContext>().RefreshTokens.CountAsync(token => token.UserId == _userId)); });
    }

    public async Task DisposeAsync() { await _services.DisposeAsync(); await _connection.DisposeAsync(); }
}