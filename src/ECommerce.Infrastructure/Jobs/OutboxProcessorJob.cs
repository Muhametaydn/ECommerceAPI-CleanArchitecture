using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.IntegrationEvents;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ECommerce.Infrastructure.Jobs
{
    /// <summary>
    /// Hangfire recurring job — OutboxMessages tablosundaki işlenmemiş mesajları
    /// MassTransit üzerinden RabbitMQ'ya yayımlar.
    ///
    /// Akış:
    ///   1. İşlenmemiş outbox mesajlarını DB'den oku (batch)
    ///   2. Her mesajı tipine göre deserialize et
    ///   3. IEventBus aracılığıyla RabbitMQ'ya yayımla
    ///   4. Başarılı mesajı ProcessedAt ile işaretle
    ///   5. Başarısız mesajda hata kaydet + retry count artır
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 30)]
    public sealed class OutboxProcessorJob
    {
        private readonly IOutboxRepository _outboxRepository;
        private readonly IEventBus _eventBus;
        private readonly ILogger<OutboxProcessorJob> _logger;

        // Desteklenen integration event tipleri
        private static readonly Dictionary<string, Type> _knownTypes = new()
        {
            [typeof(OrderCreatedIntegrationEvent).FullName!]        = typeof(OrderCreatedIntegrationEvent),
            [typeof(OrderStatusChangedIntegrationEvent).FullName!]  = typeof(OrderStatusChangedIntegrationEvent),
            [typeof(PaymentProcessedIntegrationEvent).FullName!]    = typeof(PaymentProcessedIntegrationEvent),
            [typeof(LowStockAlertIntegrationEvent).FullName!]       = typeof(LowStockAlertIntegrationEvent),
        };

        public OutboxProcessorJob(
            IOutboxRepository outboxRepository,
            IEventBus eventBus,
            ILogger<OutboxProcessorJob> logger)
        {
            _outboxRepository = outboxRepository;
            _eventBus = eventBus;
            _logger = logger;
        }

        /// <summary>Hangfire tarafından periyodik olarak çağrılır.</summary>
        public async Task ProcessAsync()
        {
            var messages = await _outboxRepository.GetUnprocessedAsync(batchSize: 50);

            if (messages.Count == 0)
                return;

            _logger.LogInformation("[OutboxProcessor] {Count} mesaj işlenecek.", messages.Count);

            foreach (var message in messages)
            {
                try
                {
                    if (!_knownTypes.TryGetValue(message.Type, out var eventType))
                    {
                        _logger.LogWarning("[OutboxProcessor] Bilinmeyen tip: {Type} | Id: {Id}", message.Type, message.Id);
                        await _outboxRepository.MarkAsFailedAsync(message.Id, $"Bilinmeyen tip: {message.Type}");
                        continue;
                    }

                    // JSON → integration event nesnesi
                    var integrationEvent = (IIntegrationEvent?)JsonSerializer.Deserialize(message.Payload, eventType);
                    if (integrationEvent is null)
                    {
                        await _outboxRepository.MarkAsFailedAsync(message.Id, "Deserialize başarısız.");
                        continue;
                    }

                    // RabbitMQ'ya yayımla (reflection ile generic metod çağrısı)
                    var publishMethod = typeof(IEventBus)
                        .GetMethod(nameof(IEventBus.PublishAsync))!
                        .MakeGenericMethod(eventType);

                    await (Task)publishMethod.Invoke(_eventBus, [integrationEvent, CancellationToken.None])!;

                    await _outboxRepository.MarkAsProcessedAsync(message.Id);

                    _logger.LogInformation(
                        "[OutboxProcessor] Yayımlandı: {Type} | Id: {Id}",
                        eventType.Name, message.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[OutboxProcessor] Hata | MessageId: {Id}", message.Id);
                    await _outboxRepository.MarkAsFailedAsync(message.Id, ex.Message);
                }
            }
        }
    }
}
