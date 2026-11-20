using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.Orders.Commands.CancelOrder;

/// <summary>
/// Sipariş iptalinin tek uygulaması: müşteri iptali ve analistin fraud reddi aynı kuralları izler
/// (stok geri eklenir, kupon hakkı geri verilir, ödeme başarısız/iade olur). Kaydetmeyi çağıran yapar.
/// </summary>
internal static class OrderCancellation
{
    public static bool CanCancel(Order order)
        => order.Status is OrderStatus.Pending or OrderStatus.Confirmed
            && !order.OrderItems.Any(item => item.Status is OrderItemStatus.Shipped or OrderItemStatus.Delivered);

    public static async Task ApplyAsync(Order order, IUnitOfWork unitOfWork)
    {
        if (order.OrderItems.Any(item => item.Status is OrderItemStatus.Shipped or OrderItemStatus.Delivered))
            throw new InvalidOperationException("Kargoya verilmiş veya teslim edilmiş ürün içeren sipariş iptal edilemez.");

        // Durum kontrolü domain'de
        order.Cancel();

        foreach (var item in order.OrderItems)
        {
            item.Cancel();
            var product = await unitOfWork.Product.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                product.IncreaseStock(item.Quantity);
                unitOfWork.Product.Update(product);
            }
        }

        if (!string.IsNullOrWhiteSpace(order.CouponCode))
        {
            var coupon = await unitOfWork.Coupon.GetByCodeAsync(order.CouponCode);
            if (coupon != null)
            {
                coupon.Release();
                unitOfWork.Coupon.Update(coupon);
            }
        }

        if (order.Payment?.Status == PaymentStatus.Pending)
            order.Payment.MarkAsFailed();
        else if (order.Payment?.Status == PaymentStatus.Completed)
            order.Payment.Refund();

        unitOfWork.Order.Update(order);
    }
}
