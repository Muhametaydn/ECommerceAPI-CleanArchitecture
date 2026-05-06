using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Categories.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Categories.Queries.GetBreadcrumb;

public class GetBreadcrumbQueryHandler
    : IRequestHandler<GetBreadcrumbQuery, BreadcrumbDTO>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public GetBreadcrumbQueryHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<BreadcrumbDTO> Handle(
        GetBreadcrumbQuery request, CancellationToken cancellationToken)
    {
        // Kategorinin var olup olmadigini kontrol et
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null)
            throw new NotFoundException(nameof(Domain.Entities.Category), request.CategoryId);

        var breadcrumbCategories = await _categoryRepository.GetBreadcrumbAsync(request.CategoryId);

        return new BreadcrumbDTO
        {
            Items = breadcrumbCategories.Select(c => new BreadcrumbItem
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Depth = c.Depth
            }).ToList()
        };
    }
}
