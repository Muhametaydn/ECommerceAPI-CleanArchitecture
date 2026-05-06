using AutoMapper;
using ECommerce.Application.Features.Categories.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Categories.Queries.GetCategoryTree;

public class GetCategoryTreeQueryHandler
    : IRequestHandler<GetCategoryTreeQuery, List<CategoryTreeDTO>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public GetCategoryTreeQueryHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<List<CategoryTreeDTO>> Handle(
        GetCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetCategoryTreeAsync();
        return _mapper.Map<List<CategoryTreeDTO>>(categories);
    }
}
