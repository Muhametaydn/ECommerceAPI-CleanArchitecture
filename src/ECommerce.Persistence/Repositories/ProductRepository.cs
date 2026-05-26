using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(Guid categoryId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .ToListAsync();
        }

        public async Task<Product?> GetProductWithReviewsAsync(Guid productId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<Product?> GetBySKUAsync(string sku)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SKU == sku);
        }

        public async Task<IReadOnlyList<Product>> GetFilteredProductsAsync(
            string? searchTerm,
            Guid? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            bool sortDescending,
            int pageNumber,
            int pageSize)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Description.ToLower().Contains(term) ||
                    p.SKU.ToLower().Contains(term));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            query = sortBy?.ToLowerInvariant() switch
            {
                "price" => sortDescending
                    ? query.OrderByDescending(p => p.Price)
                    : query.OrderBy(p => p.Price),
                "name" => sortDescending
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountFilteredProductsAsync(
            string? searchTerm,
            Guid? categoryId,
            decimal? minPrice,
            decimal? maxPrice)
        {
            var query = _dbSet.AsNoTracking().Where(p => p.IsActive).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Description.ToLower().Contains(term) ||
                    p.SKU.ToLower().Contains(term));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            return await query.CountAsync();
        }

        /// <summary>
        /// Read-only — ürün detay sayfası için (category bilgisi dahil). AsNoTracking.
        /// </summary>
        public async Task<Product?> GetProductWithCategoryAsync(Guid productId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }
    }
}
