using FluentValidation;
namespace ECommerce.Application.Features.Profiles.Commands.RequestEmailChange;
public sealed class RequestEmailChangeCommandValidator : AbstractValidator<RequestEmailChangeCommand>
{
    public RequestEmailChangeCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.NewEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.CurrentPassword).NotEmpty().MaximumLength(1024);
    }
}
