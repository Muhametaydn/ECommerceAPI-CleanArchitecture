using ECommerce.Domain.Interfaces;
using ECommerce.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IProductRepository Product { get; }
        public IOrderRepository Order { get; }
        public ICategoryRepository Category { get; }
        public ICouponRepository Coupon { get; }
        public IAddressRepository Address { get; }
        public IReviewRepository Review { get; }
        public IPaymentRepository Payment { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Product = new ProductRepository(context);
            Order = new OrderRepository(context);
            Category = new CategoryRepository(context);
            Coupon = new CouponRepository(context);
            Address = new AddressRepository(context);
            Review = new ReviewRepository(context);
            Payment = new PaymentRepository(context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
