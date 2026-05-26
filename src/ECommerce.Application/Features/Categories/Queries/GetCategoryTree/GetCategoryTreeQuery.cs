using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Categories.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Categories.Queries.GetCategoryTree;

/// <summary>Tum kategori agacini (3 seviye) getirir</summary>
public record GetCategoryTreeQuery : IRequest<List<CategoryTreeDTO>>, ICacheableRequest
{
    public string CacheKey => "categories:tree";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30); // Kategori ağacı sık değişmez
}
