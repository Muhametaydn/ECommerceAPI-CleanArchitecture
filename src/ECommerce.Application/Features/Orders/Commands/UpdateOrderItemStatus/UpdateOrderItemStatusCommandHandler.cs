using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.UpdateOrderItemStatus;

public class UpdateOrderItemStatusCommandHandler
    : IRequestHandler<UpdateOrderItemStatusCommand, OrderItemDTO>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateOrderItemStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OrderItemDTO> Handle(
        UpdateOrderItemStatusCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Order.GetOrderWithItemsAsync(request.OrderId)
            ?? throw new NotFoundException("Sipariş", request.OrderId);
        var item = order.OrderItems.FirstOrDefault(value => value.Id == request.OrderItemId)
            ?? throw new NotFoundException("Sipariş kalemi", request.OrderItemId);

        if (!request.IsAdmin && item.Product.SellerId != request.RequesterId)
            throw new ECommerce.Application.Common.Exceptions.ForbiddenException("Bu sipariş kalemini yönetme yetkiniz yok.");

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
            throw new InvalidOperationException("İptal veya iade edilmiş siparişin ürün durumu değiştirilemez.");

        switch (request.Action.ToLowerInvariant())
        {
            case "prepare":
                item.StartPreparing();
                if (order.Payment is { Status: PaymentStatus.Pending })
                    order.Payment.MarkAsCompleted($"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}");
                break;
            case "ship":
                item.Ship();
                break;
            case "deliver":
                item.Deliver();
                break;
            default:
                throw new InvalidOperationException($"Geçersiz aksiyon: {request.Action}");
        }

        order.SynchronizeStatusFromItems();
        _unitOfWork.Order.Update(order);
        await _unitOfWork.SaveChangesAsync();

        var result = _mapper.Map<OrderItemDTO>(item);
        return new OrderItemDTO
        {
            Id = result.Id,
            ProductId = result.ProductId,
            ProductName = result.ProductName,
            Quantity = result.Quantity,
            UnitPrice = result.UnitPrice,
            TotalPrice = result.TotalPrice,
            Status = result.Status,
            CanManageStatus = result.Status is not OrderItemStatus.Delivered and not OrderItemStatus.Cancelled
        };
    }
}
