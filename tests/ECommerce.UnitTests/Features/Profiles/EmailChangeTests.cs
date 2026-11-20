using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Auth.Commands.ConfirmEmailChange;
using ECommerce.Application.Features.Profiles.Commands.RequestEmailChange;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Services;
using ECommerce.Infrastructure.Settings;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace ECommerce.UnitTests.Features.Profiles;

public sealed class EmailChangeTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AccountSecurityService _service;
    private readonly Mock<IEmailService> _email = new();

    public EmailChangeTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        _provider = services.BuildServiceProvider();
        _users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
        _service = new AccountSecurityService(_users, _provider.GetRequiredService<ApplicationDbContext>(), _email.Object,
            Options.Create(new EmailSettings { FrontendBaseUrl = "https://shop.example.test" }));
    }

    private async Task<ApplicationUser> CreateUser(string email = "old@example.test")
    {
        var user = ApplicationUser.Create("Ada", "Lovelace", email, email);
        user.EmailConfirmed = true;
        (await _users.CreateAsync(user, "Current123!")).Succeeded.Should().BeTrue();
        return user;
    }

    [Fact]
    public async Task Request_WithWrongPassword_DoesNotSendOrChangeAddress()
    {
        var user = await CreateUser();
        var action = () => _service.RequestEmailChangeAsync(user.Id, "new@example.test", "WrongPassword!");
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Mevcut şifre hatalı.");
        user.Email.Should().Be("old@example.test");
        _email.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Request_WithOccupiedAddress_ReturnsWithoutSendingOrChangingAddress()
    {
        var user = await CreateUser();
        await CreateUser("taken@example.test");
        await _service.RequestEmailChangeAsync(user.Id, "taken@example.test", "Current123!");
        user.Email.Should().Be("old@example.test");
        _email.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Request_SendsAddressBoundToken_AndPreservesCurrentAddress()
    {
        var user = await CreateUser();
        string? sentUrl = null;
        _email.Setup(value => value.SendEmailChangeConfirmationAsync("new@example.test", user.FullName, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, url, _) => sentUrl = url).Returns(Task.CompletedTask);
        await _service.RequestEmailChangeAsync(user.Id, " new@example.test ", "Current123!");
        user.Email.Should().Be("old@example.test");
        user.EmailConfirmed.Should().BeTrue();
        sentUrl.Should().StartWith("https://shop.example.test/eposta-degistir?");
        var values = new Uri(sentUrl!).Query.TrimStart('?').Split('&')
            .Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1]));
        values["userId"].Should().Be(user.Id.ToString());
        (await _users.VerifyUserTokenAsync(user, _users.Options.Tokens.ChangeEmailTokenProvider, "ChangeEmail:new@example.test", values["token"])).Should().BeTrue();
        (await _users.VerifyUserTokenAsync(user, _users.Options.Tokens.ChangeEmailTokenProvider, "ChangeEmail:other@example.test", values["token"])).Should().BeFalse();
    }

    [Fact]
    public async Task Confirmation_WithTamperedAddress_IsRejectedBeforeAnyUpdate()
    {
        var user = await CreateUser();
        var token = await _users.GenerateChangeEmailTokenAsync(user, "new@example.test");
        var action = () => _service.ConfirmEmailChangeAsync(user.Id, "attacker@example.test", token);
        await action.Should().ThrowAsync<InvalidOperationException>();
        user.Email.Should().Be("old@example.test");
    }

    [Fact]
    public async Task Confirmation_WithConsumedToken_IsRejected()
    {
        var user = await CreateUser();
        var token = await _users.GenerateChangeEmailTokenAsync(user, "new@example.test");
        (await _users.ChangeEmailAsync(user, "new@example.test", token)).Succeeded.Should().BeTrue();
        var action = () => _service.ConfirmEmailChangeAsync(user.Id, "new@example.test", token);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData("not-an-email", "Current123!")]
    [InlineData("new@example.test", "")]
    public async Task Request_InvalidInput_IsRejected(string email, string password)
    {
        var result = await new RequestEmailChangeCommandValidator().ValidateAsync(new RequestEmailChangeCommand(Guid.NewGuid(), email, password));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Confirmation_MissingIdentityAndToken_IsRejected()
    {
        var result = await new ConfirmEmailChangeCommandValidator().ValidateAsync(new ConfirmEmailChangeCommand(Guid.Empty, "new@example.test", ""));
        result.IsValid.Should().BeFalse();
    }

    public void Dispose() => _provider.Dispose();
}
