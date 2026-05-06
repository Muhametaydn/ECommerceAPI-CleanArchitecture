using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.UpdateOrderStatus
{
    public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, OrderDetailDTO>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateOrderStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OrderDetailDTO> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Order.GetOrderWithItemsAsync(request.OrderId)
                ?? throw new NotFoundException("Sipariş", request.OrderId);

            switch (request.Action.ToLowerInvariant())
            {
                case "confirm":
                    order.Confirm();
                    // Ödemeyi de onayla
                    if (order.Payment != null && order.Payment.Status == Domain.Enums.PaymentStatus.Pending)
                        order.Payment.MarkAsCompleted($"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}");
                    break;

                case "ship":
                    order.Ship();
                    break;

                case "deliver":
                    order.Deliver();
                    break;

                default:
                    throw new InvalidOperationException($"Geçersiz aksiyon: {request.Action}");
            }

            _unitOfWork.Order.Update(order);
            await _unitOfWork.SaveChangesAsync();

            var updatedOrder = await _unitOfWork.Order.GetOrderWithItemsAsync(order.Id);
            return _mapper.Map<OrderDetailDTO>(updatedOrder);
        }
    }
}
