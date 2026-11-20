using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Auth.Commands.ConfirmEmail;
using ECommerce.Application.Features.Auth.Commands.RequestEmailConfirmation;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Profiles;

public sealed class EmailConfirmationCommandTests
{
    [Fact]
    public async Task ConfirmationValidators_WithValidRequests_ShouldSucceed()
    {
        var requestResult = await new RequestEmailConfirmationCommandValidator().ValidateAsync(new RequestEmailConfirmationCommand("user@example.com"));
        var confirmResult = await new ConfirmEmailCommandValidator().ValidateAsync(new ConfirmEmailCommand("user@example.com", "token"));

        requestResult.IsValid.Should().BeTrue();
        confirmResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RequestHandler_ShouldForwardEmail()
    {
        var service = new Mock<IAccountSecurityService>();
        await new RequestEmailConfirmationCommandHandler(service.Object).Handle(new RequestEmailConfirmationCommand("user@example.com"), CancellationToken.None);
        service.Verify(value => value.RequestEmailConfirmationAsync("user@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmHandler_ShouldForwardEmailAndToken()
    {
        var service = new Mock<IAccountSecurityService>();
        await new ConfirmEmailCommandHandler(service.Object).Handle(new ConfirmEmailCommand("user@example.com", "token"), CancellationToken.None);
        service.Verify(value => value.ConfirmEmailAsync("user@example.com", "token", It.IsAny<CancellationToken>()), Times.Once);
    }
}
