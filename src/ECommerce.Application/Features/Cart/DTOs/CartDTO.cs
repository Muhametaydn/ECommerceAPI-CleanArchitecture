namespace ECommerce.Application.Features.Cart.DTOs
{
    public class CartDTO
    {
        public string Id { get; init; } = string.Empty;
        public List<CartItemDTO> Items { get; init; } = new();
        public decimal TotalPrice { get; init; }
        public int TotalItems { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public class CartItemDTO
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal UnitPrice { get; init; }
        public int Quantity { get; init; }
        public decimal SubTotal { get; init; }
    }
}
