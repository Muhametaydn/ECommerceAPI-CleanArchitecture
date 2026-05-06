using FluentValidation;

namespace ECommerce.Application.Features.Addresses.Commands.CreateAddress
{
    public class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
    {
        public CreateAddressCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Kullanıcı bilgisi gereklidir.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Adres başlığı gereklidir.")
                .MaximumLength(100).WithMessage("Adres başlığı en fazla 100 karakter olabilir.");

            RuleFor(x => x.AddressLine)
                .NotEmpty().WithMessage("Adres satırı gereklidir.")
                .MaximumLength(500).WithMessage("Adres satırı en fazla 500 karakter olabilir.");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("Şehir gereklidir.")
                .MaximumLength(100);

            RuleFor(x => x.District)
                .NotEmpty().WithMessage("İlçe gereklidir.")
                .MaximumLength(100);

            RuleFor(x => x.PostalCode)
                .NotEmpty().WithMessage("Posta kodu gereklidir.")
                .MaximumLength(10);
        }
    }
}
