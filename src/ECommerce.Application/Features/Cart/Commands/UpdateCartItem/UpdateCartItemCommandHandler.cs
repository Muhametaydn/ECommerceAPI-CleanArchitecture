using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Cart.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Cart.Commands.UpdateCartItem
{
    public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, CartDTO>
    {
        private readonly ICartService _cartService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCartItemCommandHandler(ICartService cartService, IUnitOfWork unitOfWork)
        {
            _cartService = cartService;
            _unitOfWork = unitOfWork;
        }

        public async Task<CartDTO> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
        {
            var cart = await _cartService.GetCartAsync(request.CartId);
            if (cart is null)
                throw new NotFoundException("Cart", request.CartId);

            // Miktar artırılıyorsa stok kontrolü yap
            if (request.Quantity > 0)
            {
                var product = await _unitOfWork.Product.GetByIdAsync(request.ProductId);
                if (product is null)
                    throw new NotFoundException("Product", request.ProductId);

                if (request.Quantity > product.StockQuantity)
                    throw new InvalidOperationException(
                        $"Yetersiz stok. Mevcut stok: {product.StockQuantity}, İstenen: {request.Quantity}");
            }

            cart.UpdateQuantity(request.ProductId, request.Quantity);
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
