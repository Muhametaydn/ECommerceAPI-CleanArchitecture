using ECommerce.Domain.Common;

namespace ECommerce.Application.Common.Interfaces
{
    /// <summary>
    /// Domain event'lerini MediatR üzerinden dispatch eden servis arayüzü.
    /// Persistence katmanı bu arayüzü kullanarak SaveChanges sonrasında event'leri yayar.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        /// <summary>Verilen domain event listesini sırayla MediatR ile publish eder.</summary>
        Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
    }
}
