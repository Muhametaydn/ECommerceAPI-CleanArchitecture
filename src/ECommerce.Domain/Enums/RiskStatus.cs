namespace ECommerce.Domain.Enums
{
    /// <summary>
    /// Siparişin fraud değerlendirme durumu. Sipariş durum makinesinden (OrderStatus) bağımsızdır:
    /// fraud kapalıyken her sipariş NotChecked kalır ve hiçbir kuralı etkilemez.
    /// </summary>
    public enum RiskStatus
    {
        /// <summary>Değerlendirilmedi (fraud kapalı ya da karar henüz gelmedi).</summary>
        NotChecked = 0,

        /// <summary>Otomatik değerlendirme veya analist onayı: risk yok.</summary>
        Clear = 1,

        /// <summary>Analist incelemesi bekliyor.</summary>
        Review = 2,

        /// <summary>Analist reddetti.</summary>
        Rejected = 3
    }
}
