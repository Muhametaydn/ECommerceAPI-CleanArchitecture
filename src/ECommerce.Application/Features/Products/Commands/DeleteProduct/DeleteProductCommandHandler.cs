using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ISearchService _searchService;
    private readonly IProductImageStorage _imageStorage;

    public DeleteProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ISearchService searchService,
        IProductImageStorage imageStorage)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _searchService = searchService;
        _imageStorage = imageStorage;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id);

        if (product is null)
            throw new NotFoundException("Product", request.Id);

        var imageUrl = product.ImageUrl;

        _productRepository.Delete(product);
        await _unitOfWork.SaveChangesAsync();

        // Paralel: Redis cache temizle + Elasticsearch'ten kaldır
        await Task.WhenAll(
            _cacheService.RemoveAsync($"products:single:{request.Id}", cancellationToken),
            _cacheService.RemoveByPrefixAsync("products:list:", cancellationToken),
            _searchService.DeleteProductFromIndexAsync(request.Id, cancellationToken));

        await _imageStorage.DeleteIfUnreferencedAsync(imageUrl, cancellationToken);

        return true;
    }
}
