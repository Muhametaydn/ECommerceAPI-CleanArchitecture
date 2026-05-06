using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.CancelOrder
{
    /// <summary>
    /// Müşterinin kendi siparişini iptal etmesi.
    /// Sadece Pending veya Confirmed durumundaki siparişler iptal edilebilir.
    /// İptal edilen siparişlerin stokları geri eklenir.
    /// </summary>
    public class CancelOrderCommand : IRequest<Unit>
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
    }
}
