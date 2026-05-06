using FluentValidation;

namespace ECommerce.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Kategori ID'si zorunludur.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori ismi zorunludur.")
            .MaximumLength(100).WithMessage("Kategori ismi en fazla 100 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Aciklama en fazla 500 karakter olabilir.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Siralama degeri negatif olamaz.");
    }
}
