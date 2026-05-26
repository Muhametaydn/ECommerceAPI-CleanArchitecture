using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.IntegrationEvents;
using ECommerce.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Consumers
{
    /// <summary>
    /// Ödeme tamamlandığında kullanıcıya makbuz e-postası gönderir.
    /// </summary>
    public sealed class PaymentProcessedConsumer : IConsumer<PaymentProcessedIntegrationEvent>
    {
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserLookupService _userLookup;
        private readonly ILogger<PaymentProcessedConsumer> _logger;

        public PaymentProcessedConsumer(
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            IUserLookupService userLookup,
            ILogger<PaymentProcessedConsumer> logger)
        {
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _userLookup = userLookup;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentProcessedIntegrationEvent> context)
        {
            var evt = context.Message;

            _logger.LogInformation(
                "[Consumer] Ödeme tamamlandı | OrderId: {OrderId} | Tutar: {Amount:C} | TxId: {TransactionId}",
                evt.OrderId, evt.Amount, evt.TransactionId);

            // Sipariş üzerinden kullanıcıyı bul
            var order = await _unitOfWork.Order.GetByIdAsync(evt.OrderId);
            if (order is null)
            {
                _logger.LogWarning("Sipariş bulunamadı. OrderId: {OrderId}", evt.OrderId);
                return;
            }

            var userInfo = await _userLookup.GetUserInfoAsync(order.UserId, context.CancellationToken);
            if (userInfo is null)
            {
                _logger.LogWarning("Kullanıcı bulunamadı. UserId: {UserId}", order.UserId);
                return;
            }

            await _emailService.SendPaymentReceiptAsync(
                userInfo.Value.Email,
                userInfo.Value.FullName,
                evt.Amount,
                evt.TransactionId,
                context.CancellationToken);
        }
    }
}
