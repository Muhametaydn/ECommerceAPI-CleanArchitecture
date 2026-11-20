namespace ECommerce.Application.Common.Fraud;

/// <summary>
/// Fraud servisi entegrasyonunun açma/kapama ayarı (appsettings: "FraudDetection").
/// Varsayılan kapalıdır: bu durumda sipariş akışı fraud yokmuş gibi birebir aynı çalışır.
/// </summary>
public sealed class FraudDetectionSettings
{
    public const string SectionName = "FraudDetection";

    /// <summary>false: hiçbir risk olayı yayınlanmaz, hiçbir karar dinlenmez, hiçbir sipariş bekletilmez.</summary>
    public bool Enabled { get; set; }

    public FraudDetectionMode Mode { get; set; } = FraudDetectionMode.Monitor;

    /// <summary>Kararlar siparişleri gerçekten bekletiyor/iptal ediyor mu?</summary>
    public bool IsEnforcing => Enabled && Mode == FraudDetectionMode.Enforce;
}

public enum FraudDetectionMode
{
    /// <summary>İzleme: skor ve kararlar kaydedilir, hiçbir sipariş bekletilmez veya iptal edilmez.</summary>
    Monitor = 0,

    /// <summary>Uygulama: incelemedeki sipariş onaylanamaz; analist reddederse sipariş iptal edilir.</summary>
    Enforce = 1
}
