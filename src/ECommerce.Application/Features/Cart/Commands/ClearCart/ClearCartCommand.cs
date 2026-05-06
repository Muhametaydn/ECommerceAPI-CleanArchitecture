using MediatR;

namespace ECommerce.Application.Features.Cart.Commands.ClearCart
{
    public class ClearCartCommand : IRequest<Unit>
    {
        public string CartId { get; set; } = string.Empty;
    }

    public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, Unit>
    {
        private readonly Interfaces.ICartService _cartService;

        public ClearCartCommandHandler(Interfaces.ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<Unit> Handle(ClearCartCommand request, CancellationToken cancellationToken)
        {
            await _cartService.DeleteCartAsync(request.CartId);
            return Unit.Value;
        }
    }
}
