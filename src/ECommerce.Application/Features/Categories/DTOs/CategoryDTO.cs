namespace ECommerce.Application.Features.Categories.DTOs;

/// <summary>Temel kategori bilgisi — listelemelerde kullanilir</summary>
public class CategoryDTO
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int Depth { get; init; }
    public int SortOrder { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public string? ParentCategoryName { get; init; }
    public int SubCategoryCount { get; init; }
    public int ProductCount { get; init; }
}
