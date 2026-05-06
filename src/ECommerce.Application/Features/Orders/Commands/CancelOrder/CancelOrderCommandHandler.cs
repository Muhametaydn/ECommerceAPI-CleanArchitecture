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
                throw new UnauthorizedAccessException("Bu siparişi iptal etme yetkiniz yok.");

            // Domain metodu ile iptal (durum kontrolü orada yapılıyor)
            order.Cancel();

            // Stokları geri ekle
            foreach (var item in order.OrderItems)
            {
                var product = await _unitOfWork.Product.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.IncreaseStock(item.Quantity);
                    _unitOfWork.Product.Update(product);
                }
            }

            // Ödeme varsa Failed olarak işaretle
            if (order.Payment != null && order.Payment.Status == Domain.Enums.PaymentStatus.Pending)
                order.Payment.MarkAsFailed();

            _unitOfWork.Order.Update(order);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
