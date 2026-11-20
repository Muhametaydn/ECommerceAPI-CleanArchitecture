using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.UpdateOrderItemStatus;

public class UpdateOrderItemStatusCommand : IRequest<OrderItemDTO>
{
    public Guid OrderId { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid RequesterId { get; set; }
    public bool IsAdmin { get; set; }
    public string Action { get; set; } = string.Empty;
}
