using ECommerce.Application.Features.Payments.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Payments.Commands.RefundPayment
{
    /// <summary>
    /// Ödeme iadesi (Admin).
    /// Sipariş durumu Refunded olarak güncellenir, stoklar geri eklenir.
    /// </summary>
    public class RefundPaymentCommand : IRequest<PaymentDTO>
    {
        public Guid OrderId { get; set; }
    }
}
