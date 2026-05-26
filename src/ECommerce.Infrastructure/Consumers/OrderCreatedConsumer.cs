using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Consumers
{
    /// <summary>
    /// RabbitMQ'dan OrderCreatedIntegrationEvent alır → sipariş onay e-postası gönderir.
    /// </summary>
    public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedIntegrationEvent>
    {
        private readonly IEmailService _emailService;
        private readonly IUserLookupService _userLookup;
        private readonly ILogger<OrderCreatedConsumer> _logger;

        public OrderCreatedConsumer(
            IEmailService emailService,
            IUserLookupService userLookup,
            ILogger<OrderCreatedConsumer> logger)
        {
            _emailService = emailService;
            _userLookup = userLookup;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
        {
            var evt = context.Message;

            _logger.LogInformation(
                "[Consumer] Sipariş oluşturuldu | OrderNumber: {OrderNumber} | UserId: {UserId}",
                evt.OrderNumber, evt.UserId);

            var userInfo = await _userLookup.GetUserInfoAsync(evt.UserId, context.CancellationToken);
            if (userInfo is null)
            {
                _logger.LogWarning("Kullanıcı bulunamadı, e-posta gönderilemedi. UserId: {UserId}", evt.UserId);
                return;
            }

            await _emailService.SendOrderConfirmationAsync(
                userInfo.Value.Email,
                userInfo.Value.FullName,
                evt.OrderNumber,
                evt.TotalAmount,
                context.CancellationToken);
        }
    }
}
