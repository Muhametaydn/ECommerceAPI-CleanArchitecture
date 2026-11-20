using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Fraud;

/// <summary>
/// Siparişin onaylanması (ve ödemenin tamamlanması) öncesindeki fraud kontrol noktası.
/// </summary>
public interface IFraudGate
{
    /// <summary>Bekleyen bir sipariş ileri taşınmadan önce çağrılır; bekletilmesi gerekiyorsa istisna fırlatır.</summary>
    void EnsureOrderCanBeConfirmed(Order order);
}

public sealed class FraudGate : IFraudGate
{
    // Bilinçli olarak nötr: satıcıya/müşteriye "fraud şüphesi" denmez. Şüpheliyi uyarmamak
    // (tipping-off yasağı) bankacılıkta da temel kuraldır; ayrıntı yalnızca analist ekranında görünür.
    internal const string HeldMessage = "Bu sipariş ek kontrol bekliyor; şu anda onaylanamaz.";

    private readonly FraudDetectionSettings _settings;

    public FraudGate(FraudDetectionSettings settings) => _settings = settings;

    public void EnsureOrderCanBeConfirmed(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        // Kapalı veya izleme modu: hiçbir sipariş bekletilmez.
        if (!_settings.IsEnforcing)
            return;

        // Henüz karar yok (NotChecked): satış durmaz (fail-open).
        if (order.Status == OrderStatus.Pending && order.IsHeldByRiskReview)
            throw new InvalidOperationException(HeldMessage);
    }
}
