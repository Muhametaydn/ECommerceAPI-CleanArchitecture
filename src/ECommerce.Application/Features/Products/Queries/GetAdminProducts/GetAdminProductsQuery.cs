using ECommerce.Application.Features.Products.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Products.Queries.GetAdminProducts
{
    public record GetAdminProductsQuery : IRequest<IReadOnlyList<ProductDTO>>;
}
