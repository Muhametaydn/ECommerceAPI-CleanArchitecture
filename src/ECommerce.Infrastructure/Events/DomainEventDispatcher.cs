using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Notifications;
using ECommerce.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Events
{
    /// <summary>
    /// IDomainEventDispatcher implementasyonu.
    /// Domain event'leri MediatR üzerinden DomainEventNotification&lt;T&gt; olarak publish eder.
    /// Reflection kullanarak generic notification'ı çözümler.
    /// </summary>
    public sealed class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DomainEventDispatcher> _logger;

        public DomainEventDispatcher(IMediator mediator, ILogger<DomainEventDispatcher> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task DispatchAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in domainEvents)
            {
                var eventType = domainEvent.GetType();
                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
                var notification = Activator.CreateInstance(notificationType, domainEvent);

                if (notification is null)
                {
                    _logger.LogWarning("DomainEvent notification oluşturulamadı: {EventType}", eventType.Name);
                    continue;
                }

                _logger.LogDebug("Domain event dispatch ediliyor: {EventType}", eventType.Name);
                await _mediator.Publish(notification, cancellationToken);
            }
        }
    }
}
