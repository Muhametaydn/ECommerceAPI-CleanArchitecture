using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ECommerce.Infrastructure.Settings;

namespace ECommerce.Infrastructure.Consumers
{
    /// <summary>
    /// Düşük stok uyarısını admin e-posta adresine iletir.
    /// </summary>
    public sealed class LowStockAlertConsumer : IConsumer<LowStockAlertIntegrationEvent>
    {
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<LowStockAlertConsumer> _logger;

        public LowStockAlertConsumer(
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings,
            ILogger<LowStockAlertConsumer> logger)
        {
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<LowStockAlertIntegrationEvent> context)
        {
            var evt = context.Message;

            _logger.LogWarning(
                "[Consumer] DÜŞÜK STOK UYARISI | Ürün: {ProductName} | Mevcut: {CurrentStock} | Eşik: {Threshold}",
                evt.ProductName, evt.CurrentStock, evt.Threshold);

            // Düşük stok uyarısı admin adresine gider
            await _emailService.SendLowStockAlertAsync(
                _emailSettings.From, // Admin e-posta (yapılandırmadan alınabilir)
                evt.ProductName,
                evt.CurrentStock,
                context.CancellationToken);
        }
    }
}
