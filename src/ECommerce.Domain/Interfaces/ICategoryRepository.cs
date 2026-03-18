using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<IReadOnlyList<Category>> GetRootCategoriesAsync();
        Task<Category> GetCategoryWithProductsAsync(Guid categoryId);
        Task<Category> GetBySlugAsync(string slug);

    }
}
