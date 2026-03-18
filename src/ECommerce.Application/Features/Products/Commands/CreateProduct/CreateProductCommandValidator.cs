using FluentValidation;

namespace ECommerce.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı boş olamaz.")
            .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Açıklama en fazla 2000 karakter olabilir.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Fiyat 0'dan büyük olmalıdır.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stok negatif olamaz.");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU boş olamaz.")
            .MaximumLength(50).WithMessage("SKU en fazla 50 karakter olabilir.");

        RuleFor(x => x.CategoryId)
            .NotEqual(Guid.Empty).WithMessage("Kategori seçilmelidir.");
    }
}