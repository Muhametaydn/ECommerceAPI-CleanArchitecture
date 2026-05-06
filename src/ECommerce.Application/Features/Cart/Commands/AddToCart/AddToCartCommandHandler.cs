using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Cart.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Cart.Commands.AddToCart
{
    public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, CartDTO>
    {
        private readonly ICartService _cartService;
        private readonly IUnitOfWork _unitOfWork;

        public AddToCartCommandHandler(ICartService cartService, IUnitOfWork unitOfWork)
        {
            _cartService = cartService;
            _unitOfWork = unitOfWork;
        }

        public async Task<CartDTO> Handle(AddToCartCommand request, CancellationToken cancellationToken)
        {
            // Ürün var mı ve aktif mi kontrol et
            var product = await _unitOfWork.Product.GetByIdAsync(request.ProductId);
            if (product is null)
                throw new NotFoundException("Product", request.ProductId);

            if (!product.IsActive)
                throw new InvalidOperationException("Bu ürün şu anda satışta değil.");

            // Stok kontrolü
            var cart = await _cartService.GetCartAsync(request.CartId)
                       ?? new Domain.Entities.Cart
                       {
                           Id = request.CartId,
                           CreatedAt = DateTime.UtcNow
                       };

            // Sepetteki mevcut miktar + eklenen miktar stoktan fazla olmamalı
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            var totalQuantity = (existingItem?.Quantity ?? 0) + request.Quantity;

            if (totalQuantity > product.StockQuantity)
                throw new InvalidOperationException(
                    $"Yetersiz stok. Mevcut stok: {product.StockQuantity}, " +
                    $"Sepetteki miktar: {existingItem?.Quantity ?? 0}, İstenen: {request.Quantity}");

            cart.AddItem(request.ProductId, product.Name, product.Price, request.Quantity);
            await _cartService.SaveCartAsync(cart);

            return MapToDto(cart);
        }

        private static CartDTO MapToDto(Domain.Entities.Cart cart) => new()
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
