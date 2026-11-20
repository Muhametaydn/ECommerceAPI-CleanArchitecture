using AutoMapper;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Products.Queries.GetSellerProducts
{
    public class GetSellerProductsQueryHandler : IRequestHandler<GetSellerProductsQuery, IReadOnlyList<ProductDTO>>
    {
        private readonly IProductRepository _products;
        private readonly IMapper _mapper;

        public GetSellerProductsQueryHandler(IProductRepository products, IMapper mapper)
        {
            _products = products;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ProductDTO>> Handle(GetSellerProductsQuery request, CancellationToken cancellationToken)
            => _mapper.Map<IReadOnlyList<ProductDTO>>(await _products.GetSellerProductsAsync(request.SellerId));
    }
}
