namespace ECommerce.Application.Features.Reviews.DTOs
{
    public class ReviewDTO
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Comment { get; init; } = string.Empty;
        public int Rating { get; init; }
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public Guid UserId { get; init; }
        public string UserName { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    /// <summary>
    /// Ürün değerlendirme özeti
    /// </summary>
    public class ProductReviewSummaryDTO
    {
        public Guid ProductId { get; init; }
        public double AverageRating { get; init; }
        public int TotalReviews { get; init; }
        public List<ReviewDTO> Reviews { get; init; } = new();
    }
}
