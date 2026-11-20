using FluentValidation;

namespace ECommerce.Application.Features.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Token).NotEmpty().WithMessage("E-posta doğrulama bağlantısı geçersizdir.");
    }
}
