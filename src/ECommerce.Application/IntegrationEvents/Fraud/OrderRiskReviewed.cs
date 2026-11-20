// SÖZLEŞME: ECommerceFraud/src/Fraud.Contracts/OrderRiskReviewed.cs ile birebir aynı olmalı.
// Değiştirirken iki repoda birlikte güncelleyin; contract testleri contracts/samples örneklerini doğrular.
namespace ECommerce.Contracts.Fraud;

/// <summary>
/// Fraud → ECommerceAPI: bir analistin incelemeye alınmış sipariş hakkındaki nihai kararı.
/// Onay → sipariş onaylanabilir; ret → sipariş iptal edilir (stok ve kupon geri, ödeme başarısız).
/// </summary>
public sealed record OrderRiskReviewed
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public Guid EventId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }

    public Guid OrderId { get; init; }
    public Guid AssessmentId { get; init; }

    /// <summary><see cref="ReviewOutcomes"/> değerlerinden biri.</summary>
    public string Outcome { get; init; } = string.Empty;
    /// <summary>Kararı veren analistin kullanıcı kimliği (ana sistemdeki Admin).</summary>
    public Guid ReviewedBy { get; init; }
    public DateTimeOffset ReviewedAt { get; init; }
}

public static class ReviewOutcomes
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
