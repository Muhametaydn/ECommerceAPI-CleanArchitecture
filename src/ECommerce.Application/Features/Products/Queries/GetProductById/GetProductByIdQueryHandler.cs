using AutoMapper;
using MediatR;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Domain.Interfaces;
using ECommerce.Application.Common.Exceptions;
namespace ECommerce.Application.Features.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDTO>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductByIdQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<ProductDTO> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetProductWithCategoryAsync(request.Id);

        if (product is null)
            throw new NotFoundException("Product", request.Id);

        return _mapper.Map<ProductDTO>(product);
    }
}