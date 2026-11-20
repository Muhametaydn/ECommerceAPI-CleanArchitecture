using ECommerce.Application.Common.Fraud;
using ECommerce.Application.Features.Orders.Commands.CancelOrder;
using ECommerce.Contracts.Fraud;
using ECommerce.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Features.Orders.Commands.ApplyRiskDecision;

/// <summary>Analistin nihai kararını siparişe uygular (OrderRiskReviewed).</summary>
public sealed record ApplyRiskReviewCommand(Guid OrderId, Guid AssessmentId, string Outcome, Guid ReviewedBy)
    : IRequest<RiskDecisionResult>;

public sealed class ApplyRiskReviewCommandHandler : IRequestHandler<ApplyRiskReviewCommand, RiskDecisionResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FraudDetectionSettings _settings;
    private readonly ILogger<ApplyRiskReviewCommandHandler> _logger;

    public ApplyRiskReviewCommandHandler(
        IUnitOfWork unitOfWork,
        FraudDetectionSettings settings,
        ILogger<ApplyRiskReviewCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _settings = settings;
        _logger = logger;
    }

    public async Task<RiskDecisionResult> Handle(ApplyRiskReviewCommand request, CancellationToken cancellationToken)
    {
        var approved = request.Outcome switch
        {
            ReviewOutcomes.Approved => true,
            ReviewOutcomes.Rejected => false,
            _ => throw new InvalidOperationException($"Bilinmeyen inceleme sonucu: '{request.Outcome}'")
        };

        var order = await _unitOfWork.Order.GetOrderWithItemsAsync(request.OrderId);
        if (order is null)
        {
            _logger.LogWarning("Analist kararı bilinmeyen sipariş için geldi | OrderId: {OrderId}", request.OrderId);
            return RiskDecisionResult.IgnoredUnknownOrder;
        }

        if (!order.ApplyRiskReview(approved))
        {
            _logger.LogInformation("Analist kararı yok sayıldı (sipariş zaten {RiskStatus}) | OrderId: {OrderId}",
                order.RiskStatus, order.Id);
            return RiskDecisionResult.IgnoredStale;
        }

        // Ret yalnızca uygulama (Enforce) modunda siparişi iptal eder. İzleme modunda karar sadece kaydedilir:
        // amaç, kuralların gerçek trafikte müşteriyi etkilemeden ölçülmesidir.
        if (!approved && _settings.IsEnforcing)
        {
            if (OrderCancellation.CanCancel(order))
            {
                await OrderCancellation.ApplyAsync(order, _unitOfWork);
            }
            else
            {
                // Kargoya verilmiş siparişi sistem geri alamaz: operasyon ekibinin manuel müdahalesi gerekir.
                _logger.LogWarning("Reddedilen sipariş otomatik iptal edilemedi (durum: {Status}); manuel inceleme gerekli | OrderId: {OrderId}",
                    order.Status, order.Id);
            }
        }

        _unitOfWork.Order.Update(order);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Analist kararı uygulandı | OrderId: {OrderId} | Sonuç: {Outcome} | Analist: {ReviewedBy} | AssessmentId: {AssessmentId}",
            order.Id, request.Outcome, request.ReviewedBy, request.AssessmentId);
        return RiskDecisionResult.Applied;
    }
}
