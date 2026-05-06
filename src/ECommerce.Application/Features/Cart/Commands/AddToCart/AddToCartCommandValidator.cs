using FluentValidation;

namespace ECommerce.Application.Features.Cart.Commands.AddToCart;

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEqual(Guid.Empty).WithMessage("Ürün seçilmelidir.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Miktar en az 1 olmalıdır.")
            .LessThanOrEqualTo(100).WithMessage("Tek seferde en fazla 100 adet eklenebilir.");

        RuleFor(x => x.CartId)
            .NotEmpty().WithMessage("Sepet kimliği boş olamaz.");
    }
}
