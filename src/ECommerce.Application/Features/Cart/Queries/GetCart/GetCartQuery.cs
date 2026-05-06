using ECommerce.Application.Features.Cart.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Cart.Queries.GetCart
{
    public record GetCartQuery(string CartId) : IRequest<CartDTO>;

    public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDTO>
    {
        private readonly Interfaces.ICartService _cartService;

        public GetCartQueryHandler(Interfaces.ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<CartDTO> Handle(GetCartQuery request, CancellationToken cancellationToken)
        {
            var cart = await _cartService.GetCartAsync(request.CartId);

            if (cart is null)
            {
                return new CartDTO
                {
                    Id = request.CartId,
                    Items = new List<CartItemDTO>(),
                    TotalPrice = 0,
                    TotalItems = 0,
                    UpdatedAt = DateTime.UtcNow
                };
            }

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
