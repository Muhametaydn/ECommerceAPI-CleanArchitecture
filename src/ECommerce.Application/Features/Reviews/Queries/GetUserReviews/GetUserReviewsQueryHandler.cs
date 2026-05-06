using AutoMapper;
using ECommerce.Application.Features.Reviews.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Reviews.Queries.GetUserReviews
{
    public class GetUserReviewsQueryHandler : IRequestHandler<GetUserReviewsQuery, IReadOnlyList<ReviewDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetUserReviewsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ReviewDTO>> Handle(GetUserReviewsQuery request, CancellationToken cancellationToken)
        {
            var reviews = await _unitOfWork.Review.GetByUserIdAsync(request.UserId);
            return _mapper.Map<IReadOnlyList<ReviewDTO>>(reviews);
        }
    }
}
