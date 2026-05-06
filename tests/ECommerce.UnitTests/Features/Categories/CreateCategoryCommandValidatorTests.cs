using ECommerce.Application.Features.Categories.Commands.CreateCategory;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ECommerce.UnitTests.Features.Categories;

public class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator _validator;

    public CreateCategoryCommandValidatorTests()
    {
        _validator = new CreateCategoryCommandValidator();
    }

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var command = new CreateCategoryCommand
        {
            Name = "Elektronik",
            Description = "Elektronik urunler"
        };

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_ShouldFail()
    {
        var command = new CreateCategoryCommand { Name = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameTooLong_ShouldFail()
    {
        var command = new CreateCategoryCommand { Name = new string('a', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void DescriptionTooLong_ShouldFail()
    {
        var command = new CreateCategoryCommand
        {
            Name = "Test",
            Description = new string('a', 501)
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void NegativeSortOrder_ShouldFail()
    {
        var command = new CreateCategoryCommand
        {
            Name = "Test",
            SortOrder = -1
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SortOrder);
    }
}
