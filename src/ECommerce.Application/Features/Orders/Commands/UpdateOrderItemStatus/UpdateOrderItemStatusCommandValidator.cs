using FluentValidation;

namespace ECommerce.Application.Features.Orders.Commands.UpdateOrderItemStatus;

public class UpdateOrderItemStatusCommandValidator : AbstractValidator<UpdateOrderItemStatusCommand>
{
    private static readonly string[] AllowedActions = ["prepare", "ship", "deliver"];

    public UpdateOrderItemStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Sipariş ID gereklidir.");
        RuleFor(x => x.OrderItemId).NotEmpty().WithMessage("Sipariş kalemi ID gereklidir.");
        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Aksiyon belirtilmelidir.")
            .Must(action => AllowedActions.Contains(action.ToLowerInvariant()))
            .WithMessage("Geçerli aksiyonlar: prepare, ship, deliver");
    }
}
