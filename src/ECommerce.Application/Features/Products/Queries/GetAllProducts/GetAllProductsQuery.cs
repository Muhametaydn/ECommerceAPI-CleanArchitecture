using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Features.Products.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Products.Queries.GetAllProducts;

/// <summary>
/// Urun listeleme query'si — filtreleme, siralama ve sayfalama destekli.
///
/// Iki sayfalama modu destekler:
/// 1. Offset-based: PageNumber + PageSize (varsayilan, basit UI'lar icin)
/// 2. Cursor-based: Cursor + PageSize (sonsuz scroll, mobil uygulamalar icin)
///
/// Cursor gonderilirse cursor-based mod aktif olur.
/// </summary>
public class GetAllProductsQuery : IRequest<PaginatedResult<ProductDTO>>, ICacheableRequest
{
    // --- Sayfalama ---
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Cursor { get; init; }  // Cursor-based pagination icin

    // --- Filtreleme ---
    public string? SearchTerm { get; init; }
    public Guid? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? InStock { get; init; }  // Yeni: sadece stokta olanlar

    // --- Siralama ---
    public string? SortBy { get; init; }        // price, name, stock, date
    public bool SortDescending { get; init; }

    // --- Cache ---
    /// <summary>
    /// Tüm parametreler cache key'e dahil edilir; farklı filtreler farklı girdi üretir.
    /// Prefix "products:list:" ile başlar — invalidation sırasında bu prefix temizlenir.
    /// </summary>
    public string CacheKey =>
        $"products:list:p{PageNumber}s{PageSize}c{Cursor}q{SearchTerm}cat{CategoryId}" +
        $"min{MinPrice}max{MaxPrice}stock{InStock}sort{SortBy}desc{SortDescending}";

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(3);
}
