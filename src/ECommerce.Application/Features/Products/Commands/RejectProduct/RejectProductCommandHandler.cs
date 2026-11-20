using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Products.Commands.RejectProduct
{
    public class RejectProductCommandHandler : IRequestHandler<RejectProductCommand>
    {
        private readonly IProductRepository _products;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cache;
        private readonly ISearchService _search;

        public RejectProductCommandHandler(IProductRepository products, IUnitOfWork unitOfWork, ICacheService cache, ISearchService search)
        {
            _products = products;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _search = search;
        }

        public async Task Handle(RejectProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _products.GetProductForManagementAsync(request.ProductId)
                ?? throw new NotFoundException("Product", request.ProductId);
            product.Reject();
            await _unitOfWork.SaveChangesAsync();
            await Task.WhenAll(
                _cache.RemoveByPrefixAsync("products:list:", cancellationToken),
                _cache.RemoveAsync($"products:single:{product.Id}", cancellationToken),
                _search.DeleteProductFromIndexAsync(product.Id, cancellationToken));
        }
    }
}
