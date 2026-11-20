using ECommerce.Application.Features.Products.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Products.Queries.GetSellerProducts
{
    public record GetSellerProductsQuery(Guid SellerId) : IRequest<IReadOnlyList<ProductDTO>>;
}
