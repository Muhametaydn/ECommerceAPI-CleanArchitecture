namespace ECommerce.Domain.Common
{
    /// <summary>
    /// Domain event marker arayüzü.
    /// Domain katmanı saf C# kalır — MediatR bağımlılığı yoktur.
    /// Dispatch işlemi Application katmanındaki IDomainEventDispatcher üzerinden yapılır.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>Event'in benzersiz kimliği (idempotency için)</summary>
        Guid EventId { get; }

        /// <summary>Event'in oluşturulma zamanı (UTC)</summary>
        DateTime OccurredOn { get; }
    }
}
