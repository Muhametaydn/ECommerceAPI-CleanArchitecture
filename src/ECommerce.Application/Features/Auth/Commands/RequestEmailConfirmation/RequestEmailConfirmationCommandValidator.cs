using FluentValidation;

namespace ECommerce.Application.Features.Auth.Commands.RequestEmailConfirmation;

public sealed class RequestEmailConfirmationCommandValidator : AbstractValidator<RequestEmailConfirmationCommand>
{
    public RequestEmailConfirmationCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
