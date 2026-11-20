using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Profiles.Commands.UpdateProfile;
using ECommerce.Application.Features.Profiles.DTOs;
using ECommerce.Application.Features.Profiles.Queries.GetProfile;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Features.Profiles;

public sealed class ProfileHandlersTests
{
    [Fact]
    public async Task GetProfile_ShouldRequestAuthenticatedUserProfile()
    {
        var userId = Guid.NewGuid();
        var expected = CreateProfile(userId);
        var service = new Mock<IUserProfileService>();
        service.Setup(value => value.GetAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await new GetProfileQueryHandler(service.Object).Handle(new GetProfileQuery(userId), CancellationToken.None);

        result.Should().BeSameAs(expected);
        service.Verify(value => value.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_ShouldForwardOnlyCommandValues()
    {
        var userId = Guid.NewGuid();
        var expected = CreateProfile(userId);
        var service = new Mock<IUserProfileService>();
        service.Setup(value => value.UpdateAsync(userId, "Ada", "Lovelace", null, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await new UpdateProfileCommandHandler(service.Object)
            .Handle(new UpdateProfileCommand(userId, "Ada", "Lovelace", null), CancellationToken.None);

        result.Should().BeSameAs(expected);
        service.Verify(value => value.UpdateAsync(userId, "Ada", "Lovelace", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static UserProfileDTO CreateProfile(Guid userId) => new()
    {
        Id = userId,
        Email = "ada@example.com",
        FirstName = "Ada",
        LastName = "Lovelace",
        Roles = ["Customer"]
    };
}
