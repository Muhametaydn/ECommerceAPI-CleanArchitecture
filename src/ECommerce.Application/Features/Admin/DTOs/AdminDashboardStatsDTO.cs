namespace ECommerce.Application.Features.Admin.DTOs
{
    public class AdminDashboardStatsDTO
    {
        public decimal TotalRevenue { get; init; }
        public decimal ThisMonthRevenue { get; init; }
        public decimal AverageOrderValue { get; init; }
        public int TotalOrders { get; init; }
        public int PendingOrders { get; init; }
        public int ConfirmedOrders { get; init; }
        public int ShippedOrders { get; init; }
        public int DeliveredOrders { get; init; }
        public int CancelledOrders { get; init; }
        public int RefundedOrders { get; init; }
        public int TotalUsers { get; init; }
        public int ActiveUsers { get; init; }
        public int NewUsersThisMonth { get; init; }
    }
}
