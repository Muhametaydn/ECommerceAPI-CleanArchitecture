using ECommerce.Application.Features.Profiles.Commands.UpdateProfile;
using FluentAssertions;

namespace ECommerce.UnitTests.Features.Profiles;

public sealed class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidProfile_ShouldSucceed()
    {
        var result = await _validator.ValidateAsync(new UpdateProfileCommand(Guid.NewGuid(), "Ada", "Lovelace", "/uploads/profiles/avatar.webp"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyNames_ShouldFail()
    {
        var result = await _validator.ValidateAsync(new UpdateProfileCommand(Guid.NewGuid(), "", "", null));
        result.Errors.Should().Contain(error => error.PropertyName == "FirstName");
        result.Errors.Should().Contain(error => error.PropertyName == "LastName");
    }
}
