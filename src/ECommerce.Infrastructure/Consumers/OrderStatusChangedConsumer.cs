using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Consumers
{
    /// <summary>
    /// Sipariş durumu değiştiğinde kullanıcıya bildirim e-postası gönderir.
    /// </summary>
    public sealed class OrderStatusChangedConsumer : IConsumer<OrderStatusChangedIntegrationEvent>
    {
        private readonly IEmailService _emailService;
        private readonly IUserLookupService _userLookup;
        private readonly ILogger<OrderStatusChangedConsumer> _logger;

        public OrderStatusChangedConsumer(
            IEmailService emailService,
            IUserLookupService userLookup,
            ILogger<OrderStatusChangedConsumer> logger)
        {
            _emailService = emailService;
            _userLookup = userLookup;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderStatusChangedIntegrationEvent> context)
        {
            var evt = context.Message;

            _logger.LogInformation(
                "[Consumer] Sipariş durumu değişti | OrderNumber: {OrderNumber} | Yeni Durum: {Status}",
                evt.OrderNumber, evt.NewStatus);

            var userInfo = await _userLookup.GetUserInfoAsync(evt.UserId, context.CancellationToken);
            if (userInfo is null)
            {
                _logger.LogWarning("Kullanıcı bulunamadı. UserId: {UserId}", evt.UserId);
                return;
            }

            await _emailService.SendOrderStatusChangedAsync(
                userInfo.Value.Email,
                userInfo.Value.FullName,
                evt.OrderNumber,
                evt.NewStatus,
                context.CancellationToken);
        }
    }
}
