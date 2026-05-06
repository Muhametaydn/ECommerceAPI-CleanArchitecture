using ECommerce.Application.Features.Cart.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Cart.Commands.UpdateCartItem
{
    public class UpdateCartItemCommand : IRequest<CartDTO>
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public string CartId { get; set; } = string.Empty;
    }
}
