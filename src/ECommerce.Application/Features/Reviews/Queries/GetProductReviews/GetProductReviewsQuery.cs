using ECommerce.Application.Features.Reviews.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Reviews.Queries.GetProductReviews
{
    public record GetProductReviewsQuery(Guid ProductId) : IRequest<ProductReviewSummaryDTO>;
}
