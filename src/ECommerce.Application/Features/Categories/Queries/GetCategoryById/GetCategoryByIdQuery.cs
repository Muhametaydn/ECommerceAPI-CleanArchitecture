using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Categories.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Categories.Queries.GetCategoryById;

public record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDTO>, ICacheableRequest
{
    public string CacheKey => $"categories:single:{Id}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
}
