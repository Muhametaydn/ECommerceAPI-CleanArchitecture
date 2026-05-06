using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Reviews.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Reviews.Queries.GetProductReviews
{
    public class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, ProductReviewSummaryDTO>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetProductReviewsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ProductReviewSummaryDTO> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
        {
            // Ürün var mı kontrol et
            var product = await _unitOfWork.Product.GetByIdAsync(request.ProductId)
                ?? throw new NotFoundException("Ürün", request.ProductId);

            var reviews = await _unitOfWork.Review.GetByProductIdAsync(request.ProductId);
            var averageRating = await _unitOfWork.Review.GetAverageRatingAsync(request.ProductId);

            return new ProductReviewSummaryDTO
            {
                ProductId = request.ProductId,
                AverageRating = Math.Round(averageRating, 1),
                TotalReviews = reviews.Count,
                Reviews = _mapper.Map<List<ReviewDTO>>(reviews)
            };
        }
    }
}
