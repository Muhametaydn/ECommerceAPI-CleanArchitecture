using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces
{
    public interface IAddressRepository : IGenericRepository<Address>
    {
        Task<IReadOnlyList<Address>> GetByUserIdAsync(Guid userId);
    }
}
