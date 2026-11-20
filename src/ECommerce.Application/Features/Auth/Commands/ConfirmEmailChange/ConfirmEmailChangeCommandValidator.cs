using FluentValidation;
namespace ECommerce.Application.Features.Auth.Commands.ConfirmEmailChange;
public sealed class ConfirmEmailChangeCommandValidator : AbstractValidator<ConfirmEmailChangeCommand>
{
    public ConfirmEmailChangeCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.NewEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Token).NotEmpty().MaximumLength(4096);
    }
}
