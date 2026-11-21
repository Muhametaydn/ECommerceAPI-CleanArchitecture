using System.Text.Json;
using ECommerce.Application.Common.Fraud;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Contracts.Fraud;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Services;
using ECommerce.Infrastructure.Settings;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace ECommerce.IntegrationTests;

/// <summary>
/// Şifre/e-posta değişiklikleri fraud servisine "hesap ele geçirme" sinyali olarak gider; ama yalnızca
/// fraud açıkken, değişiklik gerçekten başarılı olduysa ve yeni e-posta/şifre bilgisi taşımadan.
/// </summary>
public sealed class AccountSecurityFraudEventTests : IDisposable
{
    // Gerçek transaction gerekir (servis değişiklik + token iptali + outbox'ı tek transaction'da yazar): SQLite.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _provider;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly FraudDetectionSettings _fraud = new() { Enabled = true };
    private readonly AccountSecurityService _service;

    public AccountSecurityFraudEventTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        _connection.Open();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        _provider = services.BuildServiceProvider();
        _users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
        _db = _provider.GetRequiredService<ApplicationDbContext>();
        _db.Database.EnsureCreated();
        _service = new AccountSecurityService(_users, _db, Mock.Of<IEmailService>(),
            Options.Create(new EmailSettings { FrontendBaseUrl = "https://shop.example.test" }), _fraud);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Password_change_emits_a_security_event()
    {
        var user = await CreateUser();

        await _service.ChangePasswordAsync(user.Id, "Current123!", "Changed123!");

        var message = await SingleEvent();
        message.UserId.Should().Be(user.Id);
        message.ChangeKind.Should().Be(SecurityChangeKinds.PasswordChanged);
        message.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Password_reset_emits_a_security_event()
    {
        var user = await CreateUser();
        var token = await _users.GeneratePasswordResetTokenAsync(user);

        await _service.ResetPasswordAsync(user.Email!, token, "Changed123!");

        (await SingleEvent()).ChangeKind.Should().Be(SecurityChangeKinds.PasswordReset);
    }

    [Fact]
    public async Task Email_change_emits_an_event_without_the_new_address()
    {
        var user = await CreateUser();
        var token = await _users.GenerateChangeEmailTokenAsync(user, "new@example.test");

        await _service.ConfirmEmailChangeAsync(user.Id, "new@example.test", token);

        var outbox = await _db.OutboxMessages.SingleAsync();
        outbox.Payload.Should().NotContain("new@example.test").And.NotContain("old@example.test");
        (await SingleEvent()).ChangeKind.Should().Be(SecurityChangeKinds.EmailChanged);
    }

    [Fact]
    public async Task Failed_change_emits_nothing()
    {
        var user = await CreateUser();

        await FluentActions.Invoking(() => _service.ChangePasswordAsync(user.Id, "WrongPassword!", "Changed123!"))
            .Should().ThrowAsync<InvalidOperationException>();

        (await _db.OutboxMessages.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Nothing_is_emitted_while_fraud_detection_is_off()
    {
        _fraud.Enabled = false;
        var user = await CreateUser();

        await _service.ChangePasswordAsync(user.Id, "Current123!", "Changed123!");

        (await _db.OutboxMessages.AnyAsync()).Should().BeFalse();
    }

    private async Task<ApplicationUser> CreateUser()
    {
        var user = ApplicationUser.Create("Ada", "Lovelace", "old@example.test", "old@example.test");
        user.EmailConfirmed = true;
        (await _users.CreateAsync(user, "Current123!")).Succeeded.Should().BeTrue();
        return user;
    }

    private async Task<AccountSecurityChanged> SingleEvent()
    {
        var outbox = await _db.OutboxMessages.SingleAsync();
        outbox.Type.Should().Be(typeof(AccountSecurityChanged).FullName);
        return JsonSerializer.Deserialize<AccountSecurityChanged>(outbox.Payload)!;
    }
}
