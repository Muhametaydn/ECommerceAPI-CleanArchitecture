using FluentValidation;

namespace ECommerce.Application.Features.Cart.Commands.UpdateCartItem;

public class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEqual(Guid.Empty).WithMessage("Ürün seçilmelidir.");

        RuleFor(x => x.Quantity)
            .LessThanOrEqualTo(100).WithMessage("Maksimum miktar 100 olabilir.");

        RuleFor(x => x.CartId)
            .NotEmpty().WithMessage("Sepet kimliği boş olamaz.");
    }
}
