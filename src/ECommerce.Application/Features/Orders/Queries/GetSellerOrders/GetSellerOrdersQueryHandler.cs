using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Orders.Queries.GetSellerOrders
{
    public class GetSellerOrdersQueryHandler
        : IRequestHandler<GetSellerOrdersQuery, IReadOnlyList<SellerOrderDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetSellerOrdersQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<SellerOrderDTO>> Handle(
            GetSellerOrdersQuery request,
            CancellationToken cancellationToken)
        {
            var orders = await _unitOfWork.Order.GetSellerOrdersAsync(request.SellerId);

            return orders.Select(order =>
            {
                var sellerItems = order.OrderItems
                    .Where(item => item.Product.SellerId == request.SellerId)
                    .ToArray();
                var containsOtherSellers = order.OrderItems
                    .Any(item => item.Product.SellerId != request.SellerId);

                return new SellerOrderDTO
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    Status = order.Status,
                    SellerSubtotal = sellerItems.Sum(item => item.TotalPrice),
                    SellerTotalItems = sellerItems.Sum(item => item.Quantity),
                    CanManageStatus = sellerItems.Any(item => item.Status is not Domain.Enums.OrderItemStatus.Delivered
                        and not Domain.Enums.OrderItemStatus.Cancelled),
                    ContainsOtherSellers = containsOtherSellers,
                    ShippingAddress = order.ShippingAddress.AddressLine,
                    ShippingCity = order.ShippingAddress.City,
                    ShippingDistrict = order.ShippingAddress.District,
                    Items = sellerItems.Select(item => new OrderItemDTO
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        ProductName = item.Product.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                        Status = item.Status,
                        CanManageStatus = item.Status is not Domain.Enums.OrderItemStatus.Delivered
                            and not Domain.Enums.OrderItemStatus.Cancelled
                    }).ToArray(),
                    CreatedAt = order.CreatedAt
                };
            }).ToArray();
        }
    }
}
