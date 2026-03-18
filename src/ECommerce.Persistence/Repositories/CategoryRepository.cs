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
    public class CategoryRepository : GenericRepository<Category> , ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Category>> GetRootCategoriesAsync()
        {
            return await _dbSet
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.SubCategories)
                .ToListAsync();
        
        }


        public async Task<Category?> GetCategoryWithProductsAsync(Guid categoryId)
        {
            return await _dbSet
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == categoryId);
        }

        public async Task<Category?> GetBySlugAsync(string slug)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }











    }
}
