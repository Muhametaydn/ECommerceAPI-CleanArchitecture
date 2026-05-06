using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        /// <summary>Root kategorileri alt kategorileriyle birlikte getirir</summary>
        Task<IReadOnlyList<Category>> GetRootCategoriesAsync();

        /// <summary>Tum kategori agacini 3 seviyeye kadar yukler</summary>
        Task<IReadOnlyList<Category>> GetCategoryTreeAsync();

        /// <summary>Kategoriyi urunleriyle birlikte getirir</summary>
        Task<Category?> GetCategoryWithProductsAsync(Guid categoryId);

        /// <summary>Kategoriyi alt kategorileriyle birlikte getirir</summary>
        Task<Category?> GetCategoryWithSubCategoriesAsync(Guid categoryId);

        /// <summary>Slug ile kategori getirir</summary>
        Task<Category?> GetBySlugAsync(string slug);

        /// <summary>Bir kategoriye ait breadcrumb yolunu getirir (root'a kadar)</summary>
        Task<List<Category>> GetBreadcrumbAsync(Guid categoryId);

        /// <summary>Bu slug baska bir kategoride kullaniliyor mu?</summary>
        Task<bool> SlugExistsAsync(string slug, Guid? excludeCategoryId = null);

        /// <summary>Bu kategoride veya alt kategorilerinde urun var mi?</summary>
        Task<bool> HasProductsAsync(Guid categoryId);
    }
}
