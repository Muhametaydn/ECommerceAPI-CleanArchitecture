namespace ECommerce.Application.Features.Categories.DTOs;

/// <summary>
/// Hiyerarsik kategori agaci DTO'su.
/// Her kategori kendi alt kategorilerini icerir — recursive yapida.
/// Frontend tarafinda agac (tree) gorunumu icin ideal.
/// </summary>
public class CategoryTreeDTO
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int Depth { get; init; }
    public int SortOrder { get; init; }
    public int ProductCount { get; init; }
    public List<CategoryTreeDTO> SubCategories { get; init; } = new();
}
