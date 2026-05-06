using FluentValidation;

namespace ECommerce.Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Kullanıcı bilgisi gereklidir.");

            RuleFor(x => x.CartId)
                .NotEmpty().WithMessage("Sepet bilgisi gereklidir.");

            RuleFor(x => x.ShippingAddressId)
                .NotEmpty().WithMessage("Teslimat adresi seçilmelidir.");

            RuleFor(x => x.PaymentMethod)
                .IsInEnum().WithMessage("Geçerli bir ödeme yöntemi seçilmelidir.");
        }
    }
}
