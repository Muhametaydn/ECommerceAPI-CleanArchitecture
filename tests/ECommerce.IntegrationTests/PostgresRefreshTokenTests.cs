using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Auth.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Services;
using ECommerce.Infrastructure.Settings;
using ECommerce.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;

namespace ECommerce.IntegrationTests;

// Real PostgreSQL and AuthService/Identity store. Both contexts deliberately read the same old token.
public sealed class PostgresRefreshTokenTests : IAsyncLifetime
{
    private readonly string _database = "ecommerce_refresh_" + Guid.NewGuid().ToString("N");
    private const string OldToken = "test-refresh-only";
    private string _adminConnection = null!;
    private ServiceProvider? _services;
    private bool _created;
    private Guid _userId;

    public async Task InitializeAsync()
    {
        _adminConnection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ECOMMERCE_TEST_POSTGRES")) { Database = "postgres" }.ConnectionString;
        await using (var connection = new NpgsqlConnection(_adminConnection))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", connection);
            await command.ExecuteNonQueryAsync();
            _created = true;
        }
        var target = new NpgsqlConnectionStringBuilder(_adminConnection) { Database = _database };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(target.ConnectionString));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddSingleton(Mock.Of<IAccountSecurityService>());
        services.Configure<JwtSettings>(options => { options.SecretKey = "TEST-ONLY-SECRET-AT-LEAST-32-CHARACTERS-LONG"; options.Issuer = "test"; options.Audience = "test"; options.RefreshTokenExpirationDays = 7; options.AccessTokenExpirationMinutes = 15; });
        services.AddScoped<AuthService>();
        _services = services.BuildServiceProvider();
        using var scope = _services!.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        var user = ApplicationUser.Create("Test", "Customer", "refresh@example.invalid", "refresh-test");
        user.EmailConfirmed = true;
        _userId = user.Id;
        db.Set<ApplicationUser>().Add(user);
        db.RefreshTokens.Add(RefreshToken.Create(user.Id, OldToken, 7, "127.0.0.1"));
        await db.SaveChangesAsync();
    }

    private static Task Preload(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
        .RefreshTokens.Include(token => token.User).LoadAsync();

    private static async Task<(AuthResponse? Response, Exception? Error)> Attempt(AuthService service)
    {
        try { return (await service.RefreshTokenAsync(OldToken, "127.0.0.2"), null); }
        catch (Exception exception) { return (null, exception); }
    }

    [PostgresFact]
    public async Task Concurrent_refresh_consumes_one_token_and_rolls_back_losing_replacement()
    {
        using var first = _services!.CreateScope();
        using var second = _services!.CreateScope();
        await Preload(first); await Preload(second);
        var attempts = await Task.WhenAll(Attempt(first.ServiceProvider.GetRequiredService<AuthService>()), Attempt(second.ServiceProvider.GetRequiredService<AuthService>()));
        var winner = Assert.Single(attempts, attempt => attempt.Response is not null).Response!;
        Assert.IsType<UnauthorizedAccessException>(Assert.Single(attempts, attempt => attempt.Error is not null).Error);
        using var verification = _services!.CreateScope();
        var db = verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokens = await db.RefreshTokens.AsNoTracking().ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.Single(tokens, token => token.IsActive);
        Assert.Equal(winner.RefreshToken, tokens.Single(token => token.Token == OldToken).ReplacedByToken);
        Assert.Equal(winner.RefreshToken, tokens.Single(token => token.IsActive).Token);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => verification.ServiceProvider.GetRequiredService<AuthService>().RefreshTokenAsync(OldToken, "127.0.0.3"));
        var next = await verification.ServiceProvider.GetRequiredService<AuthService>().RefreshTokenAsync(winner.RefreshToken, "127.0.0.3");
        Assert.NotEqual(winner.RefreshToken, next.RefreshToken);
        tokens = await db.RefreshTokens.AsNoTracking().ToListAsync();
        Assert.Equal(3, tokens.Count);
        Assert.Equal(next.RefreshToken, Assert.Single(tokens, token => token.IsActive).Token);
    }

    [PostgresFact]
    public async Task Stale_refresh_cannot_revive_a_token_revoked_by_logout()
    {
        using var logout = _services!.CreateScope(); using var stale = _services!.CreateScope();
        await Preload(logout); await Preload(stale);
        await logout.ServiceProvider.GetRequiredService<AuthService>().RevokeTokenAsync(OldToken);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => stale.ServiceProvider.GetRequiredService<AuthService>().RefreshTokenAsync(OldToken, "127.0.0.2"));
        using var verification = _services!.CreateScope();
        var token = await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshTokens.AsNoTracking().SingleAsync();
        Assert.True(token.IsRevoked);
        Assert.Null(token.ReplacedByToken);
    }

    [PostgresFact]
    public async Task Expired_token_is_rejected_without_creating_a_replacement()
    {
        using var scope = _services!.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var token = await db.RefreshTokens.SingleAsync();
        db.Entry(token).Property(nameof(RefreshToken.ExpiresAt)).CurrentValue = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => scope.ServiceProvider.GetRequiredService<AuthService>().RefreshTokenAsync(OldToken, "127.0.0.2"));
        Assert.Equal(1, await db.RefreshTokens.CountAsync());
        Assert.True((await db.RefreshTokens.AsNoTracking().SingleAsync()).IsExpired);
    }

    [PostgresFact]
    public async Task Revoked_token_is_rejected_without_changing_its_replacement_chain()
    {
        using var scope = _services!.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var token = await db.RefreshTokens.SingleAsync();
        token.Revoke("previous-replacement");
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => scope.ServiceProvider.GetRequiredService<AuthService>().RefreshTokenAsync(OldToken, "127.0.0.2"));
        Assert.Equal("previous-replacement", (await db.RefreshTokens.AsNoTracking().SingleAsync()).ReplacedByToken);
    }

    public async Task DisposeAsync()
    {
        if (_services is not null) await _services.DisposeAsync();
        if (!_created) return;
        if (!System.Text.RegularExpressions.Regex.IsMatch(_database, "^ecommerce_refresh_[a-f0-9]{32}$")) throw new InvalidOperationException("Invalid test database cleanup target.");
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(_adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE \"{_database}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}