using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
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
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Order>> GetOrdersByUserAsync(Guid userId) {
            return await _dbSet
                .Where (o => o.UserId == userId)
                .OrderByDescending (o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByStatusAsync(OrderStatus status)
        {
            return await _dbSet
                .Where(o => o.Status == status)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderWithItemsAsync(Guid orderId)
        {
            return await _dbSet
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
        {
            return await _dbSet
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        }









    }
}
