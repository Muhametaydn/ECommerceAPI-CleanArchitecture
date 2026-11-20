using ECommerce.Application.Features.Auth.DTOs;
using ECommerce.Domain.Constants;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Services;
using ECommerce.Infrastructure.Settings;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using ECommerce.Application.Common.Interfaces;

namespace ECommerce.UnitTests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;
    private readonly JwtSettings _jwtSettings;
    private readonly Mock<IAccountSecurityService> _accountSecurityServiceMock;

    public AuthServiceTests()
    {
        // In-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // UserManager mock
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // JWT settings
        _jwtSettings = new JwtSettings
        {
            SecretKey = "TEST-SECRET-KEY-MUST-BE-AT-LEAST-32-CHARACTERS-LONG",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        var jwtOptions = Options.Create(_jwtSettings);
        _accountSecurityServiceMock = new Mock<IAccountSecurityService>();
        _authService = new AuthService(_userManagerMock.Object, _context, jwtOptions, _accountSecurityServiceMock.Object);
    }

    // ── REGISTER TESTS ───────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsRegistrationResponseAndRequestsConfirmation()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            UserName = "testuser",
            Password = "Test1234!@#"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), AppRoles.Customer))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.RegisterAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.Message.Should().Contain("Doğrulama");
        _accountSecurityServiceMock.Verify(service => service.RequestEmailConfirmationAsync(request.Email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingUser = ApplicationUser.Create("Existing", "User", "test@example.com", "existing");

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(existingUser);

        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            UserName = "testuser",
            Password = "Test1234!@#"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(request, "127.0.0.1"));
    }

    [Fact]
    public async Task RegisterAsync_WhenCreateFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            UserName = "testuser",
            Password = "weak"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegisterAsync(request, "127.0.0.1"));
        ex.Message.Should().Contain("Password too weak");
    }

    // ── LOGIN TESTS ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var user = ApplicationUser.Create("Test", "User", "test@example.com", "testuser");
        user.EmailConfirmed = true;

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "Test1234!@#"))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { AppRoles.Customer });

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test1234!@#"
        };

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithWrongEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByEmailAsync("wrong@example.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new LoginRequest
        {
            Email = "wrong@example.com",
            Password = "Test1234!@#"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(request, "127.0.0.1"));
    }

    [Fact]
    public async Task LoginAsync_WithUnconfirmedEmail_ThrowsUnauthorizedAccessException()
    {
        var user = ApplicationUser.Create("Test", "User", "test@example.com", "testuser");
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "Test1234!@#")).ReturnsAsync(true);

        var action = () => _authService.LoginAsync(
            new LoginRequest { Email = "test@example.com", Password = "Test1234!@#" },
            "127.0.0.1");

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
        exception.Message.Should().Contain("doğrulamanız");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = ApplicationUser.Create("Test", "User", "test@example.com", "testuser");
        user.EmailConfirmed = true;

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrong"))
            .ReturnsAsync(false);

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrong"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(request, "127.0.0.1"));
    }

    [Fact]
    public async Task LoginAsync_WithDeactivatedAccount_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = ApplicationUser.Create("Test", "User", "test@example.com", "testuser");
        user.Deactivate();

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test1234!@#"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(request, "127.0.0.1"));
    }

    // ── REFRESH TOKEN TESTS ──────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokenPair()
    {
        // Arrange
        var user = ApplicationUser.Create("Test", "User", "test@example.com", "testuser");
        user.EmailConfirmed = true;
        var refreshToken = RefreshToken.Create(user.Id, "valid-token-123", 7, "127.0.0.1");

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        // ApplicationUser'i Identity tablosuna ekle (Set<> ile dogrudan eris)
        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { AppRoles.Customer });

        // Act
        var result = await _authService.RefreshTokenAsync("valid-token-123", "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe("valid-token-123"); // yeni token uretilmeli
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ThrowsUnauthorizedAccessException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RefreshTokenAsync("nonexistent-token", "127.0.0.1"));
    }

    // ── REVOKE TOKEN TESTS ───────────────────────────────────────────────────

    [Fact]
    public async Task RevokeTokenAsync_WithActiveToken_RevokesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = RefreshToken.Create(userId, "token-to-revoke", 7, "127.0.0.1");

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        // Act
        await _authService.RevokeTokenAsync("token-to-revoke", userId);

        // Assert
        var revokedToken = await _context.RefreshTokens.FirstAsync(t => t.Token == "token-to-revoke");
        revokedToken.IsActive.Should().BeFalse();
        revokedToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithInvalidToken_ThrowsUnauthorizedAccessException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RevokeTokenAsync("nonexistent-token", Guid.NewGuid()));
    }

    [Fact]
    public async Task RevokeTokenAsync_WithAlreadyRevokedToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = RefreshToken.Create(userId, "already-revoked", 7, "127.0.0.1");
        refreshToken.Revoke(); // onceden revoke et

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RevokeTokenAsync("already-revoked", userId));
    }

    // ── SECURITY HARDENING TESTS ─────────────────────────────────────────────

    [Fact]
    public async Task RevokeTokenAsync_WithAnotherUsersToken_ThrowsAndKeepsTokenActive()
    {
        var owner = Guid.NewGuid();
        _context.RefreshTokens.Add(RefreshToken.Create(owner, "victim-token", 7, "127.0.0.1"));
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RevokeTokenAsync("victim-token", Guid.NewGuid()));

        (await _context.RefreshTokens.SingleAsync()).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_RecordsFailedAttempt()
    {
        var user = ApplicationUser.Create("Test", "User", "test@example.com", "testuser");
        user.EmailConfirmed = true;
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(new LoginRequest { Email = "test@example.com", Password = "wrong" }, "127.0.0.1"));

        _userManagerMock.Verify(x => x.AccessFailedAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenLockedOut_RejectsEvenCorrectPasswordWithoutCheckingIt()
    {
        var user = ApplicationUser.Create("Test", "User", "test@example.com", "testuser");
        user.EmailConfirmed = true;
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(true);

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(new LoginRequest { Email = "test@example.com", Password = "Test1234!@#" }, "127.0.0.1"));

        error.Message.Should().Contain("Çok fazla hatalı giriş");
        _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        (await _context.RefreshTokens.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ResetsFailedAttemptCounter()
    {
        var user = ApplicationUser.Create("Test", "User", "test@example.com", "testuser");
        user.EmailConfirmed = true;
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "Test1234!@#")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { AppRoles.Customer });

        await _authService.LoginAsync(new LoginRequest { Email = "test@example.com", Password = "Test1234!@#" }, "127.0.0.1");

        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReusingRotatedTokenAfterGracePeriod_RevokesAllUserSessions()
    {
        var user = ApplicationUser.Create("Test", "User", "test@example.com", "testuser");
        user.EmailConfirmed = true;
        _context.Set<ApplicationUser>().Add(user);
        var stolen = RefreshToken.Create(user.Id, "stolen-token", 7, "127.0.0.1");
        stolen.Revoke("attacker-or-victim-current-token");
        var current = RefreshToken.Create(user.Id, "attacker-or-victim-current-token", 7, "127.0.0.1");
        _context.RefreshTokens.AddRange(stolen, current);
        await _context.SaveChangesAsync();
        _context.Entry(stolen).Property(t => t.RevokedAt).CurrentValue = DateTime.UtcNow.AddMinutes(-5);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RefreshTokenAsync("stolen-token", "10.0.0.66"));

        (await _context.RefreshTokens.AnyAsync(t => t.UserId == user.Id && t.RevokedAt == null))
            .Should().BeFalse("reuse of a rotated token means the token family is compromised");
    }

    [Fact]
    public async Task RefreshTokenAsync_ReusingJustRotatedToken_DoesNotLogOutConcurrentTab()
    {
        var user = ApplicationUser.Create("Test", "User", "test@example.com", "testuser");
        user.EmailConfirmed = true;
        _context.Set<ApplicationUser>().Add(user);
        var old = RefreshToken.Create(user.Id, "old-token", 7, "127.0.0.1");
        old.Revoke("new-token");
        _context.RefreshTokens.AddRange(old, RefreshToken.Create(user.Id, "new-token", 7, "127.0.0.1"));
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.RefreshTokenAsync("old-token", "127.0.0.1"));

        (await _context.RefreshTokens.SingleAsync(t => t.Token == "new-token")).IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    [InlineData("CHANGE-THIS-TO-A-STRONG-SECRET-KEY-AT-LEAST-32-CHARACTERS")]
    [InlineData("change-me-min-32-chars-please!!-padding")]
    public void JwtSettings_RejectsMissingShortOrPlaceholderSecret(string secret)
    {
        var settings = new JwtSettings { SecretKey = secret };

        settings.Invoking(s => s.EnsureSecretKeyIsSafe()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void JwtSettings_AcceptsRandom256BitSecret()
    {
        var settings = new JwtSettings
        {
            SecretKey = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
        };

        settings.Invoking(s => s.EnsureSecretKeyIsSafe()).Should().NotThrow();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
