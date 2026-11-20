using ECommerce.Contracts.Fraud;
using ECommerce.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Features.Orders.Commands.ApplyRiskDecision;

/// <summary>Fraud servisinin otomatik değerlendirmesini siparişe uygular (OrderRiskAssessed).</summary>
public sealed record ApplyRiskAssessmentCommand(Guid OrderId, Guid AssessmentId, string Decision, int Score)
    : IRequest<RiskDecisionResult>;

public enum RiskDecisionResult
{
    Applied,
    /// <summary>Tekrar gelen veya geç kalan mesaj: sipariş zaten daha ileri bir risk durumunda.</summary>
    IgnoredStale,
    /// <summary>Sipariş bu sistemde yok (silinmiş ya da başka ortamın mesajı).</summary>
    IgnoredUnknownOrder
}

public sealed class ApplyRiskAssessmentCommandHandler : IRequestHandler<ApplyRiskAssessmentCommand, RiskDecisionResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApplyRiskAssessmentCommandHandler> _logger;

    public ApplyRiskAssessmentCommandHandler(IUnitOfWork unitOfWork, ILogger<ApplyRiskAssessmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RiskDecisionResult> Handle(ApplyRiskAssessmentCommand request, CancellationToken cancellationToken)
    {
        var requiresReview = request.Decision switch
        {
            RiskDecisions.Clear => false,
            RiskDecisions.Review => true,
            // Bilinmeyen değer sözleşme ihlalidir: tahmin yürütmek yerine mesaj hata kuyruğuna düşsün.
            _ => throw new InvalidOperationException($"Bilinmeyen risk kararı: '{request.Decision}'")
        };

        var order = await _unitOfWork.Order.GetByIdAsync(request.OrderId);
        if (order is null)
        {
            _logger.LogWarning("Risk değerlendirmesi bilinmeyen sipariş için geldi | OrderId: {OrderId}", request.OrderId);
            return RiskDecisionResult.IgnoredUnknownOrder;
        }

        if (!order.ApplyRiskAssessment(requiresReview))
        {
            _logger.LogInformation("Risk değerlendirmesi yok sayıldı (sipariş zaten {RiskStatus}) | OrderId: {OrderId}",
                order.RiskStatus, order.Id);
            return RiskDecisionResult.IgnoredStale;
        }

        _unitOfWork.Order.Update(order);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Risk değerlendirmesi uygulandı | OrderId: {OrderId} | Karar: {Decision} | Skor: {Score} | AssessmentId: {AssessmentId}",
            order.Id, request.Decision, request.Score, request.AssessmentId);
        return RiskDecisionResult.Applied;
    }
}
