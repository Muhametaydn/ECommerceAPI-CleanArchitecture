using ECommerce.Application.Features.Categories.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Categories.Queries.GetCategoryTree;

/// <summary>Tum kategori agacini (3 seviye) getirir</summary>
public record GetCategoryTreeQuery : IRequest<List<CategoryTreeDTO>>;
