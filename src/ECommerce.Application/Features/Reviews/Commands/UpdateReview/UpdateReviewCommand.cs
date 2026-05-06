using ECommerce.Application.Features.Reviews.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Reviews.Commands.UpdateReview
{
    public class UpdateReviewCommand : IRequest<ReviewDTO>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }
    }
}
