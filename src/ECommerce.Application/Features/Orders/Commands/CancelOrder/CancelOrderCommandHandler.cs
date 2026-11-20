using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.CancelOrder
{
    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CancelOrderCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Order.GetOrderWithItemsAsync(request.OrderId)
                ?? throw new NotFoundException("Sipariş", request.OrderId);

            // Sadece kendi siparişini iptal edebilir
            if (order.UserId != request.UserId)
                throw new ForbiddenException("Bu siparişi iptal etme yetkiniz yok.");

            await OrderCancellation.ApplyAsync(order, _unitOfWork);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
