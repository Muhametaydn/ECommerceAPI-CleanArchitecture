namespace ECommerce.Application.Features.Products.DTOs;

/// <summary>
/// Elasticsearch'e index'lenen ürün belgesi.
/// Domain entity'sinden bağımsız, flat yapı.
/// </summary>
public class ProductSearchDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string SKU { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
