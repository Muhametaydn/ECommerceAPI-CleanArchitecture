// SÖZLEŞME: ECommerceFraud/src/Fraud.Contracts/AccountSecurityChanged.cs ile birebir aynı olmalı.
// Değiştirirken iki repoda birlikte güncelleyin; contract testleri contracts/samples örneklerini doğrular.
namespace ECommerce.Contracts.Fraud;

/// <summary>
/// ECommerceAPI → Fraud: hesabın şifresi veya e-postası değişti. Hesap ele geçirme kuralının sinyalidir:
/// şifre değişikliğinden hemen sonra yeni bir adrese verilen sipariş riskli sayılır.
/// Yeni e-posta, şifre veya başka kişisel veri taşınmaz; yalnızca ne değiştiği ve ne zaman.
/// </summary>
public sealed record AccountSecurityChanged
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public Guid EventId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }

    public Guid UserId { get; init; }

    /// <summary><see cref="SecurityChangeKinds"/> değerlerinden biri.</summary>
    public string ChangeKind { get; init; } = string.Empty;
}

public static class SecurityChangeKinds
{
    /// <summary>Kullanıcı mevcut şifresiyle yeni şifre belirledi.</summary>
    public const string PasswordChanged = "PasswordChanged";

    /// <summary>"Şifremi unuttum" bağlantısıyla şifre sıfırlandı.</summary>
    public const string PasswordReset = "PasswordReset";

    /// <summary>E-posta değişikliği onaylandı.</summary>
    public const string EmailChanged = "EmailChanged";
}
