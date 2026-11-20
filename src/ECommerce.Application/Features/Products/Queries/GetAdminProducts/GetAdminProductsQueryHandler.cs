using AutoMapper;
using ECommerce.Application.Features.Products.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Products.Queries.GetAdminProducts
{
    public class GetAdminProductsQueryHandler : IRequestHandler<GetAdminProductsQuery, IReadOnlyList<ProductDTO>>
    {
        private readonly IProductRepository _products;
        private readonly IMapper _mapper;

        public GetAdminProductsQueryHandler(IProductRepository products, IMapper mapper)
        {
            _products = products;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ProductDTO>> Handle(GetAdminProductsQuery request, CancellationToken cancellationToken)
            => _mapper.Map<IReadOnlyList<ProductDTO>>(await _products.GetAdminProductsAsync());
    }
}
