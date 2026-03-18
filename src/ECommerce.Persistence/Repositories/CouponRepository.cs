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
    public class CouponRepository : GenericRepository<Coupon> , ICouponRepository
    {
        public CouponRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.Code == code);
        
        }
    }
}
