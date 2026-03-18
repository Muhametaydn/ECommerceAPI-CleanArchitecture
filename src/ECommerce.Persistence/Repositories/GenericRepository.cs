using ECommerce.Domain.Common;
using ECommerce.Domain.Interfaces;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Persistence.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T> GetByIdAsync(Guid id) {
            return await _dbSet.FindAsync(id);        
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public async Task<IReadOnlyList<T>> GetAllAsync() 
        { 
            return await _dbSet.ToListAsync();
        }

        public async Task<IReadOnlyList<T>> GetWhereAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<int> CountAsync() {
            return await _dbSet.CountAsync();
        }

        public async Task<T> AddAsync(T entity) {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public void Update(T entity) { 
            _dbSet.Update(entity);
        }

        public void Delete(T entity) { 
            _dbSet.Remove(entity);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        { 
            return await _dbSet.CountAsync(predicate);    
        }





    }
}
