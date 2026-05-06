using FluentValidation;

namespace ECommerce.Application.Features.Orders.Commands.UpdateOrderStatus
{
    public class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
    {
        private static readonly string[] AllowedActions = { "confirm", "ship", "deliver" };

        public UpdateOrderStatusCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("Sipariş ID gereklidir.");

            RuleFor(x => x.Action)
                .NotEmpty().WithMessage("Aksiyon belirtilmelidir.")
                .Must(a => AllowedActions.Contains(a.ToLowerInvariant()))
                .WithMessage("Geçerli aksiyonlar: confirm, ship, deliver");
        }
    }
}
