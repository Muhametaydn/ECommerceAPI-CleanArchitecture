using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Queries.GetOrderById
{
    public record GetOrderByIdQuery(Guid OrderId, Guid UserId, bool IsAdmin = false) : IRequest<OrderDetailDTO>;
}
