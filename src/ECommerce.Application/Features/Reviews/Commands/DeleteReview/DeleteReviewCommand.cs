using MediatR;

namespace ECommerce.Application.Features.Reviews.Commands.DeleteReview
{
    public class DeleteReviewCommand : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public bool IsAdmin { get; set; }
    }
}
