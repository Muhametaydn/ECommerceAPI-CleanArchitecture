using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment?> GetByOrderIdAsync(Guid orderId);
    }
}
