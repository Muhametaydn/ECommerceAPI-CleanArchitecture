using ECommerce.Application.Features.Reviews.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Reviews.Queries.GetUserReviews
{
    public record GetUserReviewsQuery(Guid UserId) : IRequest<IReadOnlyList<ReviewDTO>>;
}
