using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(Guid categoryId);
        Task<Product?> GetProductWithReviewsAsync(Guid productId);
        Task<Product?> GetBySKUAsync(string sku);

        Task<IReadOnlyList<Product>> GetFilteredProductsAsync(
            string? searchTerm,
            Guid? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            bool sortDescending,
            int pageNumber,
            int pageSize
            );

        Task<int> CountFilteredProductsAsync(
            string? searchTerm,
            Guid? categoryId,
            decimal? minPrice,
            decimal? maxPrice);

        Task<Product?> GetProductWithCategoryAsync(Guid productId);
    }
}
