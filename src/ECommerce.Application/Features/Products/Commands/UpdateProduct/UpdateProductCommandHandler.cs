using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ISearchService _searchService;
    private readonly IMapper _mapper;

    public UpdateProductCommandHandler(
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

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id);

        if (product is null)
            throw new NotFoundException("Product", request.Id);

        product.Name = request.Name;
        product.Description = request.Description;
        product.UpdatePrice(request.Price);
        product.StockQuantity = request.StockQuantity;
        product.SKU = request.SKU;
        product.IsActive = request.IsActive;
        product.CategoryId = request.CategoryId;

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();

        // Paralel: Redis cache temizle + Elasticsearch indexini güncelle
        var searchDoc = _mapper.Map<ProductSearchDocument>(product);
        await Task.WhenAll(
            _cacheService.RemoveAsync($"products:single:{request.Id}", cancellationToken),
            _cacheService.RemoveByPrefixAsync("products:list:", cancellationToken),
            _searchService.IndexProductAsync(searchDoc, cancellationToken));

        return true;
    }
}
