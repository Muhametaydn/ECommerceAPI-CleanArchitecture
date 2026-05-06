using ECommerce.Application.Features.Cart.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Cart.Commands.MergeCart
{
    /// <summary>
    /// Anonim sepeti giriş yapan kullanıcının sepetine birleştirir.
    /// </summary>
    public class MergeCartCommand : IRequest<CartDTO>
    {
        public string AnonymousCartId { get; set; } = string.Empty;
        public string UserCartId { get; set; } = string.Empty;
    }

    public class MergeCartCommandHandler : IRequestHandler<MergeCartCommand, CartDTO>
    {
        private readonly Interfaces.ICartService _cartService;

        public MergeCartCommandHandler(Interfaces.ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<CartDTO> Handle(MergeCartCommand request, CancellationToken cancellationToken)
        {
            var mergedCart = await _cartService.MergeCartsAsync(request.AnonymousCartId, request.UserCartId);

            return new CartDTO
            {
                Id = mergedCart.Id,
                TotalPrice = mergedCart.TotalPrice,
                TotalItems = mergedCart.TotalItems,
                UpdatedAt = mergedCart.UpdatedAt,
                Items = mergedCart.Items.Select(i => new CartItemDTO
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
