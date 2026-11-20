namespace ECommerce.Application.Features.Orders.DTOs
{
    public class OrderItemDTO
    {
        public Guid Id { get; init; }
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal TotalPrice { get; init; }
        public Domain.Enums.OrderItemStatus Status { get; init; }
        public string StatusText => Status.ToString();
        public bool CanManageStatus { get; init; }
    }
}
