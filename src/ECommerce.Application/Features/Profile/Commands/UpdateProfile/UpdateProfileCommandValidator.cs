using FluentValidation;

namespace ECommerce.Application.Features.Profiles.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.FirstName).NotEmpty().WithMessage("Ad alanı zorunludur.").MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");
        RuleFor(command => command.LastName).NotEmpty().WithMessage("Soyad alanı zorunludur.").MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir.");
        RuleFor(command => command.ProfileImageUrl).MaximumLength(500).WithMessage("Profil görseli adresi en fazla 500 karakter olabilir.");
    }
}
