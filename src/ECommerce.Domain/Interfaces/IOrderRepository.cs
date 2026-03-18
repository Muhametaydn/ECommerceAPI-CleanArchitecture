using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<IReadOnlyList<Order>> GetOrdersByUserAsync(Guid userId);
        Task<IReadOnlyList<Order>> GetOrdersByStatusAsync(OrderStatus status);
        Task<Order?> GetOrderWithItemsAsync(Guid orderId);
        Task<Order?> GetByOrderNumberAsync(string orderNumber);
    }
}
