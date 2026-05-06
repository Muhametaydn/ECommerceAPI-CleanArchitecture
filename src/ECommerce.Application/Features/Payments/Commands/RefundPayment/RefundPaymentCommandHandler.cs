using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Payments.DTOs;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Payments.Commands.RefundPayment
{
    public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, PaymentDTO>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RefundPaymentCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PaymentDTO> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Order.GetOrderWithItemsAsync(request.OrderId)
                ?? throw new NotFoundException("Sipariş", request.OrderId);

            var payment = await _unitOfWork.Payment.GetByOrderIdAsync(request.OrderId)
                ?? throw new NotFoundException("Ödeme", request.OrderId);

            if (payment.Status == PaymentStatus.Refunded)
                throw new InvalidOperationException("Bu ödeme zaten iade edilmiş.");

            if (payment.Status != PaymentStatus.Completed)
                throw new InvalidOperationException("Sadece tamamlanmış ödemeler iade edilebilir.");

            // Ödeme durumunu güncelle
            payment.Status = PaymentStatus.Refunded;
            payment.UpdateAt = DateTime.UtcNow;
            _unitOfWork.Payment.Update(payment);

            // Sipariş durumunu güncelle
            order.Status = OrderStatus.Refunded;
            order.UpdateAt = DateTime.UtcNow;
            _unitOfWork.Order.Update(order);

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

            await _unitOfWork.SaveChangesAsync();

            // Güncel ödemeyi getir
            var updatedPayment = await _unitOfWork.Payment.GetByOrderIdAsync(request.OrderId);
            return _mapper.Map<PaymentDTO>(updatedPayment);
        }
    }
}
