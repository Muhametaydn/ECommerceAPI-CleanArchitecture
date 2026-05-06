using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Category>> GetRootCategoriesAsync()
        {
            return await _dbSet
                .Where(c => c.ParentCategoryId == null && c.IsActive)
                .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Category>> GetCategoryTreeAsync()
        {
            // 3 seviyeye kadar tum agaci yukle
            // Root -> Alt Kategoriler -> Alt-Alt Kategoriler
            return await _dbSet
                .Where(c => c.ParentCategoryId == null && c.IsActive)
                .Include(c => c.SubCategories.Where(sc => sc.IsActive)
                    .OrderBy(sc => sc.SortOrder))
                    .ThenInclude(sc => sc.SubCategories.Where(ssc => ssc.IsActive)
                        .OrderBy(ssc => ssc.SortOrder))
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryWithProductsAsync(Guid categoryId)
        {
            return await _dbSet
                .Include(c => c.Products.Where(p => p.IsActive))
                .FirstOrDefaultAsync(c => c.Id == categoryId);
        }

        public async Task<Category?> GetCategoryWithSubCategoriesAsync(Guid categoryId)
        {
            return await _dbSet
                .Include(c => c.SubCategories.Where(sc => sc.IsActive)
                    .OrderBy(sc => sc.SortOrder))
                .FirstOrDefaultAsync(c => c.Id == categoryId);
        }

        public async Task<Category?> GetBySlugAsync(string slug)
        {
            return await _dbSet
                .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }

        public async Task<List<Category>> GetBreadcrumbAsync(Guid categoryId)
        {
            // Root'a kadar ust kategorileri yukleyerek breadcrumb olustur
            // Ornek: Akilli Telefonlar -> Telefonlar -> Elektronik
            var breadcrumb = new List<Category>();
            var current = await _dbSet.FirstOrDefaultAsync(c => c.Id == categoryId);

            while (current != null)
            {
                breadcrumb.Insert(0, current); // basa ekle (root ilk sirada olsun)

                if (current.ParentCategoryId.HasValue)
                {
                    current = await _dbSet.FirstOrDefaultAsync(c => c.Id == current.ParentCategoryId);
                }
                else
                {
                    break;
                }
            }

            return breadcrumb;
        }

        public async Task<bool> SlugExistsAsync(string slug, Guid? excludeCategoryId = null)
        {
            var query = _dbSet.Where(c => c.Slug == slug);

            if (excludeCategoryId.HasValue)
                query = query.Where(c => c.Id != excludeCategoryId.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> HasProductsAsync(Guid categoryId)
        {
            // Bu kategori veya alt kategorilerinde urun var mi?
            var hasDirectProducts = await _context.Set<Product>()
                .AnyAsync(p => p.CategoryId == categoryId && p.IsActive);

            if (hasDirectProducts) return true;

            // Alt kategorilerdeki urunleri de kontrol et
            var subCategoryIds = await _dbSet
                .Where(c => c.ParentCategoryId == categoryId)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var subId in subCategoryIds)
            {
                if (await HasProductsAsync(subId))
                    return true;
            }

            return false;
        }
    }
}
