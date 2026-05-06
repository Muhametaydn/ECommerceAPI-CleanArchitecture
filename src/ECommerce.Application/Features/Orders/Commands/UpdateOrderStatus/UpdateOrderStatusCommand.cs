using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.UpdateOrderStatus
{
    /// <summary>
    /// Admin/Seller tarafından sipariş durumu güncelleme.
    /// Desteklenen aksiyonlar: confirm, ship, deliver
    /// </summary>
    public class UpdateOrderStatusCommand : IRequest<OrderDetailDTO>
    {
        public Guid OrderId { get; set; }

        /// <summary>
        /// Yapılacak aksiyon: "confirm", "ship", "deliver"
        /// </summary>
        public string Action { get; set; } = string.Empty;
    }
}
