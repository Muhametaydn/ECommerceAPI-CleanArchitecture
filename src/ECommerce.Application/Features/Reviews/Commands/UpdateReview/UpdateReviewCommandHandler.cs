using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Reviews.DTOs;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Reviews.Commands.UpdateReview
{
    public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, ReviewDTO>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateReviewCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ReviewDTO> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
        {
            var review = await _unitOfWork.Review.GetByIdAsync(request.Id)
                ?? throw new NotFoundException("Değerlendirme", request.Id);

            if (review.UserId != request.UserId)
                throw new UnauthorizedAccessException("Bu değerlendirmeyi güncelleme yetkiniz yok.");

            review.Title = request.Title;
            review.Comment = request.Comment;
            review.SetRating(request.Rating);

            _unitOfWork.Review.Update(review);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<ReviewDTO>(review);
        }
    }
}
