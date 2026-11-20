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
        private readonly ECommerce.Application.Common.Fraud.IFraudGate _fraudGate;

        public UpdateOrderStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ECommerce.Application.Common.Fraud.IFraudGate fraudGate)
        {
            _fraudGate = fraudGate;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OrderDetailDTO> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Order.GetOrderWithItemsAsync(request.OrderId)
                ?? throw new NotFoundException("Sipariş", request.OrderId);

            if (!request.IsAdmin)
            {
                var sellerItems = order.OrderItems
                    .Where(item => item.Product.SellerId == request.RequesterId)
                    .ToArray();

                if (sellerItems.Length == 0)
                    throw new ECommerce.Application.Common.Exceptions.ForbiddenException("Bu siparişi yönetme yetkiniz yok.");

                if (sellerItems.Length != order.OrderItems.Count)
                    throw new InvalidOperationException(
                        "Birden fazla satıcının ürününü içeren siparişlerin durumu yalnızca yönetici tarafından değiştirilebilir.");
            }

            switch (request.Action.ToLowerInvariant())
            {
                case "confirm":
                    // Onay ödemeyi de tamamlar: fraud kontrol noktası burası.
                    _fraudGate.EnsureOrderCanBeConfirmed(order);
                    order.Confirm();
                    foreach (var item in order.OrderItems.Where(item => item.Status == Domain.Enums.OrderItemStatus.Pending))
                        item.StartPreparing();
                    // Ödemeyi de onayla
                    if (order.Payment != null && order.Payment.Status == Domain.Enums.PaymentStatus.Pending)
                        order.Payment.MarkAsCompleted($"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}");
                    break;

                case "ship":
                    order.Ship();
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Status == Domain.Enums.OrderItemStatus.Pending)
                            item.StartPreparing();
                        if (item.Status == Domain.Enums.OrderItemStatus.Preparing)
                            item.Ship();
                    }
                    break;

                case "deliver":
                    order.Deliver();
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Status == Domain.Enums.OrderItemStatus.Pending)
                            item.StartPreparing();
                        if (item.Status == Domain.Enums.OrderItemStatus.Preparing)
                            item.Ship();
                        if (item.Status == Domain.Enums.OrderItemStatus.Shipped)
                            item.Deliver();
                    }
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
