using ECommerce.Application.Common.Fraud;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Features.Fraud;

public class FraudGateAndRiskStatusTests
{
    // ── Order risk durumu (idempotent, sırasız mesajlara dayanıklı) ──────────

    [Theory]
    [InlineData(false, RiskStatus.Clear)]
    [InlineData(true, RiskStatus.Review)]
    public void Assessment_moves_an_unchecked_order_to_clear_or_review(bool requiresReview, RiskStatus expected)
    {
        var order = NewOrder();

        order.ApplyRiskAssessment(requiresReview).Should().BeTrue();

        order.RiskStatus.Should().Be(expected);
    }

    [Fact]
    public void Duplicate_or_late_assessment_is_ignored()
    {
        var order = NewOrder();
        order.ApplyRiskAssessment(requiresReview: true);
        order.ApplyRiskReview(approved: true);

        // At-least-once teslimat: aynı değerlendirme tekrar gelir ya da analist kararından sonra ulaşır.
        order.ApplyRiskAssessment(requiresReview: true).Should().BeFalse();

        order.RiskStatus.Should().Be(RiskStatus.Clear);
    }

    [Theory]
    [InlineData(true, RiskStatus.Clear)]
    [InlineData(false, RiskStatus.Rejected)]
    public void Analyst_decision_resolves_a_review(bool approved, RiskStatus expected)
    {
        var order = NewOrder();
        order.ApplyRiskAssessment(requiresReview: true);

        order.ApplyRiskReview(approved).Should().BeTrue();

        order.RiskStatus.Should().Be(expected);
    }

    [Fact]
    public void Analyst_decision_arriving_before_the_assessment_still_counts()
    {
        var order = NewOrder();

        order.ApplyRiskReview(approved: false).Should().BeTrue();
        order.ApplyRiskAssessment(requiresReview: false).Should().BeFalse();

        order.RiskStatus.Should().Be(RiskStatus.Rejected);
    }

    [Fact]
    public void Rejected_order_cannot_be_approved_later()
    {
        var order = NewOrder();
        order.ApplyRiskReview(approved: false);

        order.ApplyRiskReview(approved: true).Should().BeFalse();

        order.RiskStatus.Should().Be(RiskStatus.Rejected);
        order.IsHeldByRiskReview.Should().BeTrue();
    }

    // ── Fraud kontrol noktası (onay) ─────────────────────────────────────────

    [Theory]
    [InlineData(false, FraudDetectionMode.Enforce)] // kapalı
    [InlineData(true, FraudDetectionMode.Monitor)]  // izleme
    public void Off_and_monitor_modes_never_hold_an_order(bool enabled, FraudDetectionMode mode)
    {
        var gate = Gate(enabled, mode);

        foreach (var order in new[] { InReview(), Rejected() })
            FluentActions.Invoking(() => gate.EnsureOrderCanBeConfirmed(order)).Should().NotThrow();
    }

    [Fact]
    public void Enforce_mode_holds_orders_in_review_or_rejected()
    {
        var gate = Gate(true, FraudDetectionMode.Enforce);

        FluentActions.Invoking(() => gate.EnsureOrderCanBeConfirmed(InReview())).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => gate.EnsureOrderCanBeConfirmed(Rejected())).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Enforce_mode_fails_open_when_no_decision_has_arrived()
    {
        var gate = Gate(true, FraudDetectionMode.Enforce);
        var unchecked_ = NewOrder();
        var clear = NewOrder();
        clear.ApplyRiskAssessment(requiresReview: false);

        FluentActions.Invoking(() => gate.EnsureOrderCanBeConfirmed(unchecked_)).Should().NotThrow();
        FluentActions.Invoking(() => gate.EnsureOrderCanBeConfirmed(clear)).Should().NotThrow();
    }

    [Fact]
    public void Held_message_does_not_tip_off_the_customer_or_seller()
    {
        var gate = Gate(true, FraudDetectionMode.Enforce);

        var error = FluentActions.Invoking(() => gate.EnsureOrderCanBeConfirmed(InReview()))
            .Should().Throw<InvalidOperationException>().Which;

        error.Message.Should().Contain("ek kontrol");
        error.Message.ToLowerInvariant().Should().NotContainAny("fraud", "dolandırıcılık", "şüpheli", "risk");
    }

    [Fact]
    public void Settings_are_off_by_default()
    {
        var settings = new FraudDetectionSettings();

        settings.Enabled.Should().BeFalse();
        settings.IsEnforcing.Should().BeFalse();
    }

    private static IFraudGate Gate(bool enabled, FraudDetectionMode mode)
        => new FraudGate(new FraudDetectionSettings { Enabled = enabled, Mode = mode });

    private static Order NewOrder() => new() { Id = Guid.NewGuid(), OrderNumber = "ORD-TEST", UserId = Guid.NewGuid() };

    private static Order InReview()
    {
        var order = NewOrder();
        order.ApplyRiskAssessment(requiresReview: true);
        return order;
    }

    private static Order Rejected()
    {
        var order = NewOrder();
        order.ApplyRiskReview(approved: false);
        return order;
    }
}
