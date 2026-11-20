using FluentValidation;

namespace ECommerce.Application.Features.Profiles.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.CurrentPassword).NotEmpty().WithMessage("Mevcut şifre zorunludur.");
        RuleFor(command => command.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre zorunludur.")
            .MinimumLength(8).WithMessage("Yeni şifre en az 8 karakter olmalıdır.")
            .Matches("[a-z]").WithMessage("Yeni şifre en az bir küçük harf içermelidir.")
            .Matches("[A-Z]").WithMessage("Yeni şifre en az bir büyük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Yeni şifre en az bir rakam içermelidir.")
            .Matches("[^A-Za-z0-9]").WithMessage("Yeni şifre en az bir özel karakter içermelidir.")
            .NotEqual(command => command.CurrentPassword).WithMessage("Yeni şifre mevcut şifreden farklı olmalıdır.");
    }
}
