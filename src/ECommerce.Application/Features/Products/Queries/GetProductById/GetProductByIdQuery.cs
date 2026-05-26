using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Products.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDTO>, ICacheableRequest
{
    public string CacheKey => $"products:single:{Id}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}
