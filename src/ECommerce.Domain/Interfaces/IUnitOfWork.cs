using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Product { get; }
        IOrderRepository Order { get; }
        ICategoryRepository Category { get; }
        ICouponRepository Coupon { get; }
        IAddressRepository Address { get; }
        IReviewRepository Review { get; }
        IPaymentRepository Payment { get; }

        Task<int> SaveChangesAsync();

    }
}
