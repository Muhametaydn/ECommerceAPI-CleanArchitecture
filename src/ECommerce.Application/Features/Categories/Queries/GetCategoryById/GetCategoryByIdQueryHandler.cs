using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Categories.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandler
    : IRequestHandler<GetCategoryByIdQuery, CategoryDTO>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<CategoryDTO> Handle(
        GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetCategoryWithSubCategoriesAsync(request.Id);

        if (category == null)
            throw new NotFoundException(nameof(Domain.Entities.Category), request.Id);

        return _mapper.Map<CategoryDTO>(category);
    }
}
