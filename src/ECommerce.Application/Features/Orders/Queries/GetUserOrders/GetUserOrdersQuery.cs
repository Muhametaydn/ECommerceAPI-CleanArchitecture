using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Queries.GetUserOrders
{
    public record GetUserOrdersQuery(Guid UserId) : IRequest<IReadOnlyList<OrderDTO>>;
}
