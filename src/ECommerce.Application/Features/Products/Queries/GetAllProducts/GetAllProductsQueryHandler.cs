using AutoMapper;
using MediatR;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Application.Features.Products.Specifications;
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
        // Cursor'i Guid'e cevir (geldiyse)
        Guid? cursorId = null;
        if (!string.IsNullOrEmpty(request.Cursor) && Guid.TryParse(request.Cursor, out var parsedCursor))
        {
            cursorId = parsedCursor;
        }

        // 1. Filtreleme + siralama + sayfalama icin Specification olustur
        var spec = new ProductFilterSpecification(
            searchTerm: request.SearchTerm,
            categoryId: request.CategoryId,
            minPrice: request.MinPrice,
            maxPrice: request.MaxPrice,
            inStock: request.InStock,
            sortBy: request.SortBy,
            sortDescending: request.SortDescending,
            pageNumber: cursorId.HasValue ? null : request.PageNumber,
            pageSize: request.PageSize,
            cursor: cursorId);

        // 2. Count icin ayni kriterlere sahip ama sayfalama/siralama olmayan spec
        var countSpec = new ProductFilterSpecification(
            searchTerm: request.SearchTerm,
            categoryId: request.CategoryId,
            minPrice: request.MinPrice,
            maxPrice: request.MaxPrice,
            inStock: request.InStock);

        // 3. Specification ile sorgula — filtreleme mantigi artik tek yerde
        var products = await _productRepository.ListAsync(spec);
        var totalCount = await _productRepository.CountAsync(countSpec);

        var productDtos = _mapper.Map<List<ProductDTO>>(products);

        return new PaginatedResult<ProductDTO>(
            productDtos, totalCount, request.PageNumber, request.PageSize);
    }
}
