using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Products.Commands.ApproveProduct
{
    public class ApproveProductCommandHandler : IRequestHandler<ApproveProductCommand>
    {
        private readonly IProductRepository _products;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cache;
        private readonly ISearchService _search;
        private readonly IMapper _mapper;

        public ApproveProductCommandHandler(IProductRepository products, IUnitOfWork unitOfWork, ICacheService cache, ISearchService search, IMapper mapper)
        {
            _products = products;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _search = search;
            _mapper = mapper;
        }

        public async Task Handle(ApproveProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _products.GetProductForManagementAsync(request.ProductId)
                ?? throw new NotFoundException("Product", request.ProductId);
            product.Approve();
            await _unitOfWork.SaveChangesAsync();
            await Task.WhenAll(
                _cache.RemoveByPrefixAsync("products:list:", cancellationToken),
                _cache.RemoveAsync($"products:single:{product.Id}", cancellationToken),
                _search.IndexProductAsync(_mapper.Map<ProductSearchDocument>(product), cancellationToken));
        }
    }
}
