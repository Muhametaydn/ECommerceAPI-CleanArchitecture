using AutoMapper;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ISearchService _searchService;
    private readonly IMapper _mapper;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ISearchService searchService,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _searchService = searchService;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            SKU = request.SKU,
            CategoryId = request.CategoryId,
        };

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        // Paralel: Redis cache temizle + Elasticsearch indexle
        var searchDoc = _mapper.Map<ProductSearchDocument>(product);
        await Task.WhenAll(
            _cacheService.RemoveByPrefixAsync("products:list:", cancellationToken),
            _searchService.IndexProductAsync(searchDoc, cancellationToken));

        return product.Id;
    }
}
