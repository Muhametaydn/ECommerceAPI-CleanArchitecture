using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Admin.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services
{
    public sealed class AdminQueryService : IAdminQueryService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public AdminQueryService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<AdminUserDTO>> GetUsersAsync(
            CancellationToken cancellationToken = default)
        {
            var users = await _userManager.Users
                .AsNoTracking()
                .OrderByDescending(user => user.CreatedAt)
                .ToListAsync(cancellationToken);

            var result = new List<AdminUserDTO>(users.Count);
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new AdminUserDTO
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToArray()
                });
            }

            return result;
        }

        public async Task<AdminDashboardStatsDTO> GetDashboardStatsAsync(
            CancellationToken cancellationToken = default)
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var orders = _dbContext.Orders.AsNoTracking();
            var deliveredOrders = orders.Where(order => order.Status == OrderStatus.Delivered);

            var totalRevenue = await deliveredOrders
                .Select(order => (decimal?)order.TotalAmount)
                .SumAsync(cancellationToken) ?? 0;
            var deliveredCount = await deliveredOrders.CountAsync(cancellationToken);

            return new AdminDashboardStatsDTO
            {
                TotalRevenue = totalRevenue,
                ThisMonthRevenue = await deliveredOrders
                    .Where(order => order.CreatedAt >= monthStart)
                    .Select(order => (decimal?)order.TotalAmount)
                    .SumAsync(cancellationToken) ?? 0,
                AverageOrderValue = deliveredCount == 0 ? 0 : totalRevenue / deliveredCount,
                TotalOrders = await orders.CountAsync(cancellationToken),
                PendingOrders = await orders.CountAsync(order => order.Status == OrderStatus.Pending, cancellationToken),
                ConfirmedOrders = await orders.CountAsync(order => order.Status == OrderStatus.Confirmed, cancellationToken),
                ShippedOrders = await orders.CountAsync(order => order.Status == OrderStatus.Shipped, cancellationToken),
                DeliveredOrders = deliveredCount,
                CancelledOrders = await orders.CountAsync(order => order.Status == OrderStatus.Cancelled, cancellationToken),
                RefundedOrders = await orders.CountAsync(order => order.Status == OrderStatus.Refunded, cancellationToken),
                TotalUsers = await _userManager.Users.CountAsync(cancellationToken),
                ActiveUsers = await _userManager.Users.CountAsync(user => user.IsActive, cancellationToken),
                NewUsersThisMonth = await _userManager.Users.CountAsync(user => user.CreatedAt >= monthStart, cancellationToken)
            };
        }
    }
}
