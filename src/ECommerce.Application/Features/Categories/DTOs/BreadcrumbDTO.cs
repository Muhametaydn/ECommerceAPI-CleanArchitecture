namespace ECommerce.Application.Features.Categories.DTOs;

/// <summary>
/// Breadcrumb (kırıntı yolu) DTO'su.
/// Ornek: Elektronik > Telefonlar > Akıllı Telefonlar
/// Frontend'de navigasyon icin kullanilir.
/// </summary>
public class BreadcrumbDTO
{
    public List<BreadcrumbItem> Items { get; init; } = new();
}

public class BreadcrumbItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int Depth { get; init; }
}
