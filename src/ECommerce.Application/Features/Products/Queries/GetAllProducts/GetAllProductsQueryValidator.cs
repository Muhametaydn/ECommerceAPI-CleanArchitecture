using FluentValidation;

namespace ECommerce.Application.Features.Products.Queries.GetAllProducts;

public class GetAllProductsQueryValidator : AbstractValidator<GetAllProductsQuery>
{
    public GetAllProductsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Sayfa numarasi 1'den kucuk olamaz.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("Sayfa boyutu 1-50 arasinda olmali.");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice.HasValue)
            .WithMessage("Minimum fiyat 0'dan kucuk olamaz.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice.HasValue)
            .WithMessage("Maksimum fiyat 0'dan kucuk olamaz.");

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("Minimum fiyat, maksimum fiyattan buyuk olamaz.");

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) ||
                            new[] { "price", "name", "stock", "date" }
                                .Contains(sortBy.ToLowerInvariant()))
            .WithMessage("Gecerli siralama alanlari: price, name, stock, date");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .When(x => x.SearchTerm != null)
            .WithMessage("Arama terimi en fazla 100 karakter olabilir.");
    }
}
