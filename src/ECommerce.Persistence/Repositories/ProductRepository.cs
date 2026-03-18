using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Persistence.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context) 
        {
        }

        public async Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(Guid categoryId) 
        {
            return  await _dbSet
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
        }

        public async Task<Product?> GetProductWithReviewsAsync(Guid productId) { 
            return await _dbSet
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<Product?> GetBySKUAsync(string sku)
        {
            return await _dbSet
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
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            // Arama
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Description.ToLower().Contains(term) ||
                    p.SKU.ToLower().Contains(term));
            }

            // Kategori
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            // Fiyat aralığı
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Sıralama
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

            // Sayfalama
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
            var query = _dbSet.Where(p => p.IsActive).AsQueryable();

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

        public async Task<Product?> GetProductWithCategoryAsync(Guid productId)
        {
            return await _dbSet
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }



    }
}
