using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Interfaces
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        //Tekli sorgular
        Task<T> GetByIdAsync(Guid id);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        //Coklu sorgular
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<IReadOnlyList<T>> GetWhereAsync(Expression<Func<T, bool>> predicate);

        //Sayim
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync();

        //Create update Delete
        Task<T> AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);

        

        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    }
}
