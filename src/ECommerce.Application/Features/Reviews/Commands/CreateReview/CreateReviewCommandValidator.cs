using FluentValidation;

namespace ECommerce.Application.Features.Reviews.Commands.CreateReview
{
    public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
    {
        public CreateReviewCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Kullanıcı bilgisi gereklidir.");

            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Ürün bilgisi gereklidir.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Başlık gereklidir.")
                .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir.");

            RuleFor(x => x.Comment)
                .MaximumLength(2000).WithMessage("Yorum en fazla 2000 karakter olabilir.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Puan 1-5 arasında olmalıdır.");
        }
    }
}
