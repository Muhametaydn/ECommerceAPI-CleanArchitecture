using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces
{
    public interface IReviewRepository : IGenericRepository<Review>
    {
        Task<IReadOnlyList<Review>> GetByProductIdAsync(Guid productId);
        Task<IReadOnlyList<Review>> GetByUserIdAsync(Guid userId);
        Task<bool> HasUserReviewedProductAsync(Guid userId, Guid productId);
        Task<double> GetAverageRatingAsync(Guid productId);
    }
}
