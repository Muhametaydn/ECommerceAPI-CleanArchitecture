using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Queries.GetSellerOrders
{
    public record GetSellerOrdersQuery(Guid SellerId) : IRequest<IReadOnlyList<SellerOrderDTO>>;
}
