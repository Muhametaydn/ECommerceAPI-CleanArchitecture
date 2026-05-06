using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Reviews.Commands.DeleteReview
{
    public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteReviewCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
        {
            var review = await _unitOfWork.Review.GetByIdAsync(request.Id)
                ?? throw new NotFoundException("Değerlendirme", request.Id);

            // Admin her değerlendirmeyi silebilir, kullanıcı sadece kendininkileri
            if (!request.IsAdmin && review.UserId != request.UserId)
                throw new UnauthorizedAccessException("Bu değerlendirmeyi silme yetkiniz yok.");

            _unitOfWork.Review.Delete(review);
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
