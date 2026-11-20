using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Auth.Commands.ForgotPassword;
using ECommerce.Application.Features.Auth.Commands.ResetPassword;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Profiles;

public sealed class PasswordResetCommandTests
{
    [Fact]
    public async Task ForgotPasswordValidator_WithValidEmail_ShouldSucceed()
    {
        var result = await new ForgotPasswordCommandValidator().ValidateAsync(new ForgotPasswordCommand("user@example.com"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPasswordValidator_WithInvalidEmail_ShouldFail()
    {
        var result = await new ForgotPasswordCommandValidator().ValidateAsync(new ForgotPasswordCommand("invalid"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordValidator_WithStrongPasswordAndToken_ShouldSucceed()
    {
        var result = await new ResetPasswordCommandValidator().ValidateAsync(new ResetPasswordCommand("user@example.com", "token", "NewPass2!"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPasswordHandler_ShouldForwardEmail()
    {
        var service = new Mock<IAccountSecurityService>();
        await new ForgotPasswordCommandHandler(service.Object).Handle(new ForgotPasswordCommand("user@example.com"), CancellationToken.None);
        service.Verify(value => value.RequestPasswordResetAsync("user@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordHandler_ShouldForwardCredentials()
    {
        var service = new Mock<IAccountSecurityService>();
        await new ResetPasswordCommandHandler(service.Object).Handle(new ResetPasswordCommand("user@example.com", "token", "NewPass2!"), CancellationToken.None);
        service.Verify(value => value.ResetPasswordAsync("user@example.com", "token", "NewPass2!", It.IsAny<CancellationToken>()), Times.Once);
    }
}
