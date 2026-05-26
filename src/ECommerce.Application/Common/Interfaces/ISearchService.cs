using ECommerce.Application.Features.Products.DTOs;

namespace ECommerce.Application.Common.Interfaces;

public interface ISearchService
{
    /// <summary>Full-text product search via Elasticsearch</summary>
    Task<ProductSearchResult> SearchProductsAsync(
        string? query,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        bool? inStock,
        string? sortBy,
        bool sortDescending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Ürünü Elasticsearch indexine ekle / güncelle</summary>
    Task IndexProductAsync(ProductSearchDocument document, CancellationToken cancellationToken = default);

    /// <summary>Ürünü Elasticsearch indexinden kaldır</summary>
    Task DeleteProductFromIndexAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Tüm ürünleri yeniden indexle (admin reindex)</summary>
    Task ReindexAllAsync(IEnumerable<ProductSearchDocument> documents, CancellationToken cancellationToken = default);
}
