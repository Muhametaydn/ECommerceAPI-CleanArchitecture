using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Orders.Commands.CancelOrder;
using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Application.Features.Payments.Commands.RefundPayment;
using ECommerce.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Common.Behaviors;

// Runs only after a committed stock mutation. Cache failure must not disguise a successful order.
public sealed class StockCacheInvalidationBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork, ICacheService cache,
    ILogger<StockCacheInvalidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);
        Guid? orderId = request switch
        {
            CreateOrderCommand when response is OrderDetailDTO created => created.Id,
            CancelOrderCommand cancelled => cancelled.OrderId,
            RefundPaymentCommand refunded => refunded.OrderId,
            _ => null
        };
        if (orderId is null) return response;
        try
        {
            var order = await unitOfWork.Order.GetOrderWithItemsAsync(orderId.Value);
            if (order is null) return response;
            var removals = order.OrderItems.Select(item => item.ProductId).Distinct()
                .Select(id => cache.RemoveAsync($"products:single:{id}", CancellationToken.None)).ToList();
            removals.Add(cache.RemoveByPrefixAsync("products:list:", CancellationToken.None));
            await Task.WhenAll(removals);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Committed order {OrderId}: product stock cache invalidation failed", orderId);
        }
        return response;
    }
}
