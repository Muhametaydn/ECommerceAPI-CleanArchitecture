using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Queries.GetAllOrders
{
    public record GetAllOrdersQuery : IRequest<IReadOnlyList<OrderDTO>>;
}
