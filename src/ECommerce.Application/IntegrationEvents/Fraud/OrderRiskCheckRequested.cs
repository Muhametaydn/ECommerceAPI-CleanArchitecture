// SÖZLEŞME: ECommerceFraud/src/Fraud.Contracts/OrderRiskCheckRequested.cs ile birebir aynı olmalı.
// Değiştirirken iki repoda birlikte güncelleyin; contract testleri contracts/samples örneklerini doğrular.
namespace ECommerce.Contracts.Fraud;

/// <summary>
/// ECommerceAPI → Fraud: yeni bir siparişin risk değerlendirmesi istenir.
/// Fraud servisi ana veritabanını okumaz; karar için gereken her şey bu olayla gelir.
/// </summary>
/// <remarks>
/// KVKK / veri minimizasyonu: tam e-posta ve açık adres gönderilmez. E-postanın yalnızca alan adı,
/// adresin ise normalize edilmiş özetinin (SHA-256) gönderilmesi, "aynı adres birçok hesapta mı?"
/// kontrolü için yeterlidir.
/// </remarks>
public sealed record OrderRiskCheckRequested
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public Guid EventId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }

    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;

    public RiskCustomer Customer { get; init; } = new();
    public RiskChannel Channel { get; init; } = new();
    public RiskShippingAddress ShippingAddress { get; init; } = new();
    public IReadOnlyList<RiskOrderItem> Items { get; init; } = [];

    /// <summary>İndirim sonrası ödenecek tutar.</summary>
    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    /// <summary>ISO 4217 kodu, örn. "TRY".</summary>
    public string Currency { get; init; } = "TRY";
    /// <summary><see cref="PaymentMethods"/> değerlerinden biri.</summary>
    public string PaymentMethod { get; init; } = string.Empty;
    public string? CouponCode { get; init; }
}

public sealed record RiskCustomer
{
    public Guid UserId { get; init; }
    public DateTimeOffset AccountCreatedAt { get; init; }
    /// <summary>Yalnızca alan adı (örn. "outlook.com"), tam e-posta değil.</summary>
    public string EmailDomain { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
}

public sealed record RiskChannel
{
    /// <summary>Güvenilir proxy zincirinden çözümlenmiş istemci IP'si.</summary>
    public string IpAddress { get; init; } = string.Empty;
    public string? UserAgent { get; init; }
}

public sealed record RiskShippingAddress
{
    public Guid AddressId { get; init; }
    /// <summary>Normalize edilmiş açık adres + posta kodunun SHA-256 özeti (hex).</summary>
    public string Fingerprint { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    /// <summary>ISO 3166-1 alpha-2, örn. "TR".</summary>
    public string CountryCode { get; init; } = string.Empty;
}

public sealed record RiskOrderItem
{
    public Guid ProductId { get; init; }
    public Guid CategoryId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public static class PaymentMethods
{
    public const string CreditCard = "CreditCard";
    public const string DebitCard = "DebitCard";
    public const string BankTransfer = "BankTransfer";
}
