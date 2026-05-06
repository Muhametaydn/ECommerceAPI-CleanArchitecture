using ECommerce.Domain.Common;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.Specifications;
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

        // --- Specification Pattern Implementasyonu ---

        /// <summary>
        /// Specification'daki tum kosullari (WHERE, Include, OrderBy, Paging)
        /// IQueryable uzerine uygular ve sonucu doner.
        /// </summary>
        public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).ToListAsync();
        }

        public async Task<int> CountAsync(ISpecification<T> spec)
        {
            // Count icin paging ve ordering uygulamiyoruz — sadece criteria
            var query = _dbSet.AsQueryable();

            if (spec.Criteria != null)
                query = query.Where(spec.Criteria);

            return await query.CountAsync();
        }

        public async Task<T?> FirstOrDefaultAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Specification Evaluator: Specification'daki tum kurallari
        /// IQueryable'a cevirir. Bu method sayesinde her repository
        /// ayri ayri filtreleme yazmak zorunda kalmaz.
        /// </summary>
        private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            var query = _dbSet.AsQueryable();

            // 1. WHERE kosulu
            if (spec.Criteria != null)
                query = query.Where(spec.Criteria);

            // 2. Expression-based Include'lar (p => p.Category)
            query = spec.Includes.Aggregate(query,
                (current, include) => current.Include(include));

            // 3. String-based Include'lar ("Category.SubCategories")
            query = spec.IncludeStrings.Aggregate(query,
                (current, include) => current.Include(include));

            // 4. Siralama
            if (spec.OrderBy != null)
                query = query.OrderBy(spec.OrderBy);
            else if (spec.OrderByDescending != null)
                query = query.OrderByDescending(spec.OrderByDescending);

            // 5. Sayfalama
            if (spec.IsPagingEnabled)
            {
                if (spec.Skip.HasValue)
                    query = query.Skip(spec.Skip.Value);
                if (spec.Take.HasValue)
                    query = query.Take(spec.Take.Value);
            }

            return query;
        }
    }
}
