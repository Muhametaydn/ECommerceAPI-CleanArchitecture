using AutoMapper;
using MediatR;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.Products.Queries.GetAllProducts;

public class GetAllProductsQueryHandler
    : IRequestHandler<GetAllProductsQuery, PaginatedResult<ProductDTO>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetAllProductsQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<ProductDTO>> Handle(
        GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetFilteredProductsAsync(
            request.SearchTerm,
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            request.SortBy,
            request.SortDescending,
            request.PageNumber,
            request.PageSize);

        var totalCount = await _productRepository.CountFilteredProductsAsync(
            request.SearchTerm,
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice);

        var productDtos = _mapper.Map<List<ProductDTO>>(products);

        return new PaginatedResult<ProductDTO>(
            productDtos, totalCount, request.PageNumber, request.PageSize);
    }
}