using ECommerce.Domain.Common;
using MediatR;

namespace ECommerce.Application.Common.Notifications
{
    /// <summary>
    /// Domain event'i MediatR INotification'a saran generic wrapper.
    /// Domain katmanı MediatR'a bağımlı kalmadan, Application katmanı MediatR pipeline'ını kullanabilir.
    /// </summary>
    /// <typeparam name="TDomainEvent">Herhangi bir IDomainEvent implementasyonu</typeparam>
    public sealed class DomainEventNotification<TDomainEvent> : INotification
        where TDomainEvent : IDomainEvent
    {
        public TDomainEvent DomainEvent { get; }

        public DomainEventNotification(TDomainEvent domainEvent)
        {
            DomainEvent = domainEvent;
        }
    }
}
