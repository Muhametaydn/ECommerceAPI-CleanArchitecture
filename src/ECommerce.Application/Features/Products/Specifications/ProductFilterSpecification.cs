using System.Linq.Expressions;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Specifications;

namespace ECommerce.Application.Features.Products.Specifications;

/// <summary>
/// Urun filtreleme, siralama ve sayfalama mantigi tek bir yerde.
///
/// Avantajlar:
/// 1. DRY — Ayni filtre hem liste hem count sorgusunda kullanilir
/// 2. Testlenebilir — Specification'i birim testi yazabilirsin
/// 3. Genisletilebilir — Yeni filtre eklemek icin sadece buraya gel
/// 4. Okunabilir — Repository temiz kalir
/// </summary>
public class ProductFilterSpecification : BaseSpecification<Product>
{
    public ProductFilterSpecification(
        string? searchTerm = null,
        Guid? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStock = null,
        string? sortBy = null,
        bool sortDescending = false,
        int? pageNumber = null,
        int? pageSize = null,
        Guid? cursor = null)
    {
        // --- KRITERLERI OLUSTUR ---
        Expression<Func<Product, bool>> criteria = p => p.IsActive;

        // Arama — isim, aciklama veya SKU icinde arar
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            criteria = CombineAnd(criteria, p =>
                p.Name.ToLower().Contains(term) ||
                p.Description.ToLower().Contains(term) ||
                p.SKU.ToLower().Contains(term));
        }

        // Kategori filtresi
        if (categoryId.HasValue)
        {
            criteria = CombineAnd(criteria, p => p.CategoryId == categoryId.Value);
        }

        // Fiyat araligi
        if (minPrice.HasValue)
        {
            criteria = CombineAnd(criteria, p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            criteria = CombineAnd(criteria, p => p.Price <= maxPrice.Value);
        }

        // Stok filtresi
        if (inStock == true)
        {
            criteria = CombineAnd(criteria, p => p.StockQuantity > 0);
        }

        // Cursor-based pagination: "Bu ID'den sonraki kayitlar"
        // Varsayilan siralama CreatedAt DESC oldugu icin cursor ID'si olan kayittan
        // onceki tarihe sahip kayitlari getiriyoruz.
        // Not: Basit cursor implementasyonu — ID bazli
        if (cursor.HasValue)
        {
            criteria = CombineAnd(criteria, p => p.Id.CompareTo(cursor.Value) > 0);
        }

        AddCriteria(criteria);

        // --- INCLUDE'LAR ---
        AddInclude(p => p.Category);

        // --- SIRALAMA ---
        ApplySorting(sortBy, sortDescending);

        // --- SAYFALAMA ---
        if (cursor.HasValue && pageSize.HasValue)
        {
            // Cursor-based: offset yok, sadece Take
            ApplyPaging(0, pageSize.Value);
        }
        else if (pageNumber.HasValue && pageSize.HasValue)
        {
            // Offset-based: geleneksel Skip/Take
            ApplyPaging((pageNumber.Value - 1) * pageSize.Value, pageSize.Value);
        }
    }

    private void ApplySorting(string? sortBy, bool descending)
    {
        switch (sortBy?.ToLowerInvariant())
        {
            case "price":
                if (descending) ApplyOrderByDescending(p => p.Price);
                else ApplyOrderBy(p => p.Price);
                break;
            case "name":
                if (descending) ApplyOrderByDescending(p => p.Name);
                else ApplyOrderBy(p => p.Name);
                break;
            case "stock":
                if (descending) ApplyOrderByDescending(p => p.StockQuantity);
                else ApplyOrderBy(p => p.StockQuantity);
                break;
            default:
                // Varsayilan: en yeni urunler once
                ApplyOrderByDescending(p => p.CreatedAt);
                break;
        }
    }

    /// <summary>
    /// Iki Expression'i AND ile birlestirir.
    /// </summary>
    private static Expression<Func<Product, bool>> CombineAnd(
        Expression<Func<Product, bool>> left,
        Expression<Func<Product, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(Product), "p");

        var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
        var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);

        var combined = Expression.AndAlso(leftBody, rightBody);
        return Expression.Lambda<Func<Product, bool>>(combined, parameter);
    }

    private static Expression ReplaceParameter(
        Expression body, ParameterExpression oldParam, ParameterExpression newParam)
    {
        return new ParameterReplacer(oldParam, newParam).Visit(body);
    }

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParam;
        private readonly ParameterExpression _newParam;

        public ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
        {
            _oldParam = oldParam;
            _newParam = newParam;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _oldParam ? _newParam : base.VisitParameter(node);
    }
}
