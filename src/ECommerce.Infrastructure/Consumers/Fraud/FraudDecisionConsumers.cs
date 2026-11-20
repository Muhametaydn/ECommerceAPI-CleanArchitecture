using ECommerce.Application.Features.Orders.Commands.ApplyRiskDecision;
using ECommerce.Contracts.Fraud;
using MassTransit;
using MediatR;

namespace ECommerce.Infrastructure.Consumers.Fraud;

/// <summary>
/// Fraud servisinden gelen kararları uygular. Consumer'lar ince tutulur: mesajı komuta çevirir,
/// iş kuralları Application katmanındadır. Yalnızca FraudDetection:Enabled=true iken kaydedilir.
/// </summary>
public sealed class OrderRiskAssessedConsumer : IConsumer<OrderRiskAssessed>
{
    private readonly IMediator _mediator;

    public OrderRiskAssessedConsumer(IMediator mediator) => _mediator = mediator;

    public Task Consume(ConsumeContext<OrderRiskAssessed> context)
    {
        var message = context.Message;
        return _mediator.Send(
            new ApplyRiskAssessmentCommand(message.OrderId, message.AssessmentId, message.Decision, message.Score),
            context.CancellationToken);
    }
}

public sealed class OrderRiskReviewedConsumer : IConsumer<OrderRiskReviewed>
{
    private readonly IMediator _mediator;

    public OrderRiskReviewedConsumer(IMediator mediator) => _mediator = mediator;

    public Task Consume(ConsumeContext<OrderRiskReviewed> context)
    {
        var message = context.Message;
        return _mediator.Send(
            new ApplyRiskReviewCommand(message.OrderId, message.AssessmentId, message.Outcome, message.ReviewedBy),
            context.CancellationToken);
    }
}
