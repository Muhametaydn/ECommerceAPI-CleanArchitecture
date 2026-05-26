using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Categories.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Categories.Queries.GetCategoryBySlug;

public record GetCategoryBySlugQuery(string Slug) : IRequest<CategoryDTO>, ICacheableRequest
{
    public string CacheKey => $"categories:slug:{Slug}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
}
