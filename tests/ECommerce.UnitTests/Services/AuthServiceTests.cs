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

namespace ECommerce.UnitTests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;
    private readonly JwtSettings _jwtSettings;

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
        _authService = new AuthService(_userManagerMock.Object, _context, jwtOptions);
    }

    // ── REGISTER TESTS ───────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsAuthResponse()
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

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { AppRoles.Customer });

        // Act
        var result = await _authService.RegisterAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.FullName.Should().Be("Test User");
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Roles.Should().Contain(AppRoles.Customer);
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
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = ApplicationUser.Create("Test", "User", "test@example.com", "testuser");

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
        await _authService.RevokeTokenAsync("token-to-revoke");

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
            () => _authService.RevokeTokenAsync("nonexistent-token"));
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
            () => _authService.RevokeTokenAsync("already-revoked"));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
