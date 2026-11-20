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
    private readonly IMarketplaceSettingsService _marketplaceSettings;
    private readonly IProductImageStorage _imageStorage;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ISearchService searchService,
        IMapper mapper,
        IMarketplaceSettingsService marketplaceSettings,
        IProductImageStorage imageStorage)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _searchService = searchService;
        _mapper = mapper;
        _marketplaceSettings = marketplaceSettings;
        _imageStorage = imageStorage;
    }

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetProductForManagementAsync(request.Id);

        if (product is null)
            throw new NotFoundException("Product", request.Id);

        if (!request.IsAdmin && product.SellerId != request.RequesterId)
            throw new ECommerce.Application.Common.Exceptions.ForbiddenException("Bu ürünü düzenleme yetkiniz bulunmuyor.");

        var previousImageUrl = product.ImageUrl;

        product.Name = request.Name;
        product.Description = request.Description;
        product.UpdatePrice(request.Price);
        product.StockQuantity = request.StockQuantity;
        product.SKU = request.SKU;
        product.CategoryId = request.CategoryId;
        product.UpdateImage(request.ImageUrl);

        if (request.IsAdmin)
        {
            product.IsActive = product.ApprovalStatus == Domain.Enums.ProductApprovalStatus.Approved && request.IsActive;
        }
        else
        {
            var settings = await _marketplaceSettings.GetAsync(cancellationToken);
            if (settings.AutoApproveSellerProducts)
            {
                product.Approve();
                product.IsActive = request.IsActive;
            }
            else
            {
                product.RequireApproval();
            }
        }

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();

        await Task.WhenAll(
            _cacheService.RemoveAsync($"products:single:{request.Id}", cancellationToken),
            _cacheService.RemoveByPrefixAsync("products:list:", cancellationToken));

        if (product.IsActive && product.ApprovalStatus == Domain.Enums.ProductApprovalStatus.Approved)
            await _searchService.IndexProductAsync(_mapper.Map<ProductSearchDocument>(product), cancellationToken);
        else
            await _searchService.DeleteProductFromIndexAsync(product.Id, cancellationToken);

        if (!string.Equals(previousImageUrl, product.ImageUrl, StringComparison.OrdinalIgnoreCase))
            await _imageStorage.DeleteIfUnreferencedAsync(previousImageUrl, cancellationToken);

        return true;
    }
}
