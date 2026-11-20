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
                throw new ECommerce.Application.Common.Exceptions.ForbiddenException("Bu siparişi iptal etme yetkiniz yok.");

            if (order.OrderItems.Any(item => item.Status is Domain.Enums.OrderItemStatus.Shipped
                or Domain.Enums.OrderItemStatus.Delivered))
                throw new InvalidOperationException("Kargoya verilmiş veya teslim edilmiş ürün içeren sipariş iptal edilemez.");

            // Domain metodu ile iptal (durum kontrolü orada yapılıyor)
            order.Cancel();

            // Stokları geri ekle
            foreach (var item in order.OrderItems)
            {
                item.Cancel();
                var product = await _unitOfWork.Product.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.IncreaseStock(item.Quantity);
                    _unitOfWork.Product.Update(product);
                }
            }

            // İptal edilen sipariş kuponun kullanım hakkını geri verir

            if (!string.IsNullOrWhiteSpace(order.CouponCode))

            {

                var coupon = await _unitOfWork.Coupon.GetByCodeAsync(order.CouponCode);

                if (coupon != null)

                {

                    coupon.Release();

                    _unitOfWork.Coupon.Update(coupon);

                }

            }


            // Ödeme varsa Failed olarak işaretle
            if (order.Payment != null && order.Payment.Status == Domain.Enums.PaymentStatus.Pending)
                order.Payment.MarkAsFailed();

            if (order.Payment?.Status == Domain.Enums.PaymentStatus.Completed)
                order.Payment.Refund();

            _unitOfWork.Order.Update(order);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
