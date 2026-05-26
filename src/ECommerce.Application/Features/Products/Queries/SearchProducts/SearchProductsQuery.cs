using ECommerce.Application.Features.Products.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Products.Queries.SearchProducts;

/// <summary>
/// Elasticsearch üzerinden full-text ürün arama query'si.
/// Büyük kataloglarda filtreleme ve arama için DB sorgusu yerine ES kullanılır.
/// </summary>
public record SearchProductsQuery(
    string? Query,
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? InStock,
    string? SortBy,
    bool SortDescending = false,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<ProductSearchResult>;
