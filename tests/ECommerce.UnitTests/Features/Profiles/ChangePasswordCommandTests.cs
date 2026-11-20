using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Profiles.Commands.ChangePassword;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Profiles;

public sealed class ChangePasswordCommandTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithStrongDifferentPassword_ShouldSucceed()
    {
        var result = await _validator.ValidateAsync(new ChangePasswordCommand(Guid.NewGuid(), "OldPass1!", "NewPass2!"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoNumber!")]
    [InlineData("NoSpecial1")]
    public async Task Validate_WithWeakPassword_ShouldFail(string newPassword)
    {
        var result = await _validator.ValidateAsync(new ChangePasswordCommand(Guid.NewGuid(), "OldPass1!", newPassword));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldForwardAuthenticatedUserAndPasswords()
    {
        var userId = Guid.NewGuid();
        var service = new Mock<IAccountSecurityService>();
        var command = new ChangePasswordCommand(userId, "OldPass1!", "NewPass2!");

        await new ChangePasswordCommandHandler(service.Object).Handle(command, CancellationToken.None);

        service.Verify(value => value.ChangePasswordAsync(userId, "OldPass1!", "NewPass2!", It.IsAny<CancellationToken>()), Times.Once);
    }
}
