using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Products.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Products.Queries.SearchProducts;

public class SearchProductsQueryHandler
    : IRequestHandler<SearchProductsQuery, ProductSearchResult>
{
    private readonly ISearchService _searchService;

    public SearchProductsQueryHandler(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task<ProductSearchResult> Handle(
        SearchProductsQuery request, CancellationToken cancellationToken)
    {
        return await _searchService.SearchProductsAsync(
            query: request.Query,
            categoryId: request.CategoryId,
            minPrice: request.MinPrice,
            maxPrice: request.MaxPrice,
            inStock: request.InStock,
            sortBy: request.SortBy,
            sortDescending: request.SortDescending,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);
    }
}
