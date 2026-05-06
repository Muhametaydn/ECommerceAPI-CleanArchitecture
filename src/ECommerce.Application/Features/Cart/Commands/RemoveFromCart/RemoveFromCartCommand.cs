using ECommerce.Application.Features.Cart.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Cart.Commands.RemoveFromCart
{
    public class RemoveFromCartCommand : IRequest<CartDTO>
    {
        public Guid ProductId { get; set; }
        public string CartId { get; set; } = string.Empty;
    }

    public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, CartDTO>
    {
        private readonly Interfaces.ICartService _cartService;

        public RemoveFromCartCommandHandler(Interfaces.ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<CartDTO> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
        {
            var cart = await _cartService.GetCartAsync(request.CartId);
            if (cart is null)
                return new CartDTO { Id = request.CartId };

            cart.RemoveItem(request.ProductId);
            await _cartService.SaveCartAsync(cart);

            return new CartDTO
            {
                Id = cart.Id,
                TotalPrice = cart.TotalPrice,
                TotalItems = cart.TotalItems,
                UpdatedAt = cart.UpdatedAt,
                Items = cart.Items.Select(i => new CartItemDTO
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    SubTotal = i.SubTotal
                }).ToList()
            };
        }
    }
}
