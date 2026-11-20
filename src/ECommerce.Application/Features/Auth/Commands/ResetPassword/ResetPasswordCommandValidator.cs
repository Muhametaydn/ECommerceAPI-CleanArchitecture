using FluentValidation;

namespace ECommerce.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("E-posta adresi zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256);
        RuleFor(command => command.Token).NotEmpty().WithMessage("Şifre sıfırlama bağlantısı geçersizdir.");
        RuleFor(command => command.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre zorunludur.")
            .MinimumLength(8).WithMessage("Yeni şifre en az 8 karakter olmalıdır.")
            .Matches("[a-z]").WithMessage("Yeni şifre en az bir küçük harf içermelidir.")
            .Matches("[A-Z]").WithMessage("Yeni şifre en az bir büyük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Yeni şifre en az bir rakam içermelidir.")
            .Matches("[^A-Za-z0-9]").WithMessage("Yeni şifre en az bir özel karakter içermelidir.");
    }
}
