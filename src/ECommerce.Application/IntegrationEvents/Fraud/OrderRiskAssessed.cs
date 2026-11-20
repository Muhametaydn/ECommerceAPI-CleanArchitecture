// SÖZLEŞME: ECommerceFraud/src/Fraud.Contracts/OrderRiskAssessed.cs ile birebir aynı olmalı.
// Değiştirirken iki repoda birlikte güncelleyin; contract testleri contracts/samples örneklerini doğrular.
namespace ECommerce.Contracts.Fraud;

/// <summary>
/// Fraud → ECommerceAPI: otomatik değerlendirmenin sonucu.
/// Fraud servisi asla otomatik ret vermez; en fazla incelemeye alır. Ret kararını analist verir.
/// </summary>
public sealed record OrderRiskAssessed
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public Guid EventId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }

    public Guid OrderId { get; init; }
    public Guid AssessmentId { get; init; }

    /// <summary>0–100 arası toplam risk puanı.</summary>
    public int Score { get; init; }
    /// <summary><see cref="RiskLevels"/> değerlerinden biri.</summary>
    public string RiskLevel { get; init; } = string.Empty;
    /// <summary><see cref="RiskDecisions"/> değerlerinden biri.</summary>
    public string Decision { get; init; } = string.Empty;

    /// <summary>Kararın açıklaması: tetiklenen kurallar, puana katkısına göre azalan sırada.</summary>
    public IReadOnlyList<RiskReason> Reasons { get; init; } = [];

    /// <summary>Kararı üreten kural seti / politika sürümü (denetim için).</summary>
    public string PolicyVersion { get; init; } = string.Empty;
}

public sealed record RiskReason
{
    /// <summary>Kalıcı kural kodu, örn. "VELOCITY_USER_1H".</summary>
    public string RuleCode { get; init; } = string.Empty;
    public int Points { get; init; }
    public string Description { get; init; } = string.Empty;
}

public static class RiskLevels
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
}

public static class RiskDecisions
{
    public const string Clear = "Clear";
    public const string Review = "Review";
}
