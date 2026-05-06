using ECommerce.Domain.Enums;
using FluentValidation;

namespace ECommerce.Application.Features.Coupons.Commands.CreateCoupon
{
    public class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
    {
        public CreateCouponCommandValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Kupon kodu gereklidir.")
                .MinimumLength(3).WithMessage("Kupon kodu en az 3 karakter olmalı.")
                .MaximumLength(50).WithMessage("Kupon kodu en fazla 50 karakter olabilir.");

            RuleFor(x => x.DiscountType)
                .IsInEnum().WithMessage("Geçerli bir indirim tipi seçilmelidir.");

            RuleFor(x => x.DiscountValue)
                .GreaterThan(0).WithMessage("İndirim değeri 0'dan büyük olmalıdır.");

            RuleFor(x => x.DiscountValue)
                .LessThanOrEqualTo(100)
                .When(x => x.DiscountType == DiscountType.Percentage)
                .WithMessage("Yüzdelik indirim 100'den büyük olamaz.");

            RuleFor(x => x.MaxUsageCount)
                .GreaterThan(0).WithMessage("Maksimum kullanım sayısı 0'dan büyük olmalıdır.");

            RuleFor(x => x.ExpiryDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Son kullanma tarihi gelecekte olmalıdır.");

            RuleFor(x => x.MinimumOrderAmount)
                .GreaterThan(0).When(x => x.MinimumOrderAmount.HasValue)
                .WithMessage("Minimum sipariş tutarı 0'dan büyük olmalıdır.");
        }
    }
}
