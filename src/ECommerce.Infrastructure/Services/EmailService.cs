using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Settings;
using FluentEmail.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services
{
    public sealed class EmailService : IEmailService
    {
        private readonly IFluentEmail _fluentEmail;
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IFluentEmail fluentEmail, IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _fluentEmail = fluentEmail;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendOrderConfirmationAsync(string toEmail, string toName, string orderNumber, decimal totalAmount, CancellationToken ct = default)
        {
            var body = BuildOrderConfirmationBody(toName, orderNumber, totalAmount);

            await SendAsync(toEmail, toName, $"Siparişiniz Alındı — #{orderNumber}", body, ct);
        }

        public async Task SendOrderStatusChangedAsync(string toEmail, string toName, string orderNumber, string newStatus, CancellationToken ct = default)
        {
            var statusTr = TranslateStatus(newStatus);
            var body = BuildStatusChangedBody(toName, orderNumber, statusTr);

            await SendAsync(toEmail, toName, $"Sipariş Güncelleme — #{orderNumber} {statusTr}", body, ct);
        }

        public async Task SendPaymentReceiptAsync(string toEmail, string toName, decimal amount, string transactionId, CancellationToken ct = default)
        {
            var body = BuildPaymentReceiptBody(toName, amount, transactionId);

            await SendAsync(toEmail, toName, "Ödeme Makbuzunuz", body, ct);
        }

        public async Task SendLowStockAlertAsync(string adminEmail, string productName, int currentStock, CancellationToken ct = default)
        {
            var body = BuildLowStockBody(productName, currentStock);

            await SendAsync(adminEmail, "Admin", $"⚠️ Düşük Stok Uyarısı — {productName}", body, ct);
        }

        // ── Gönderim ────────────────────────────────────────────────────────────
        private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct)
        {
            try
            {
                var result = await _fluentEmail
                    .To(toEmail, toName)
                    .Subject(subject)
                    .Body(htmlBody, isHtml: true)
                    .SendAsync(ct);

                if (result.Successful)
                    _logger.LogInformation("E-posta gönderildi → {To} | Konu: {Subject}", toEmail, subject);
                else
                    _logger.LogWarning("E-posta gönderilemedi → {To} | Hatalar: {Errors}", toEmail, string.Join(", ", result.ErrorMessages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta gönderilirken hata oluştu → {To}", toEmail);
            }
        }

        // ── E-posta şablonları ─────────────────────────────────────────────────
        private static string BuildOrderConfirmationBody(string name, string orderNumber, decimal total) => $"""
            <html><body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:auto">
              <h2 style="color:#2d7dd2">Siparişiniz Alındı! 🎉</h2>
              <p>Merhaba <strong>{name}</strong>,</p>
              <p>Siparişiniz başarıyla oluşturuldu. Hazırlanmaya başlandığında sizi bilgilendireceğiz.</p>
              <table style="width:100%;border-collapse:collapse;margin:20px 0">
                <tr style="background:#f5f5f5"><td style="padding:10px"><strong>Sipariş No</strong></td><td style="padding:10px">#{orderNumber}</td></tr>
                <tr><td style="padding:10px"><strong>Toplam Tutar</strong></td><td style="padding:10px">{total:C2}</td></tr>
              </table>
              <p style="color:#888;font-size:12px">Bu e-posta otomatik gönderilmiştir, lütfen yanıtlamayınız.</p>
            </body></html>
            """;

        private static string BuildStatusChangedBody(string name, string orderNumber, string statusTr) => $"""
            <html><body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:auto">
              <h2 style="color:#2d7dd2">Sipariş Durumu Güncellendi</h2>
              <p>Merhaba <strong>{name}</strong>,</p>
              <p><strong>#{orderNumber}</strong> numaralı siparişinizin durumu güncellendi.</p>
              <div style="background:#f0f7ff;border-left:4px solid #2d7dd2;padding:15px;margin:20px 0">
                <strong>Yeni Durum:</strong> {statusTr}
              </div>
              <p style="color:#888;font-size:12px">Bu e-posta otomatik gönderilmiştir, lütfen yanıtlamayınız.</p>
            </body></html>
            """;

        private static string BuildPaymentReceiptBody(string name, decimal amount, string transactionId) => $"""
            <html><body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:auto">
              <h2 style="color:#28a745">Ödemeniz Alındı ✓</h2>
              <p>Merhaba <strong>{name}</strong>,</p>
              <p>Ödemeniz başarıyla işlendi.</p>
              <table style="width:100%;border-collapse:collapse;margin:20px 0">
                <tr style="background:#f5f5f5"><td style="padding:10px"><strong>İşlem Kimliği</strong></td><td style="padding:10px">{transactionId}</td></tr>
                <tr><td style="padding:10px"><strong>Tutar</strong></td><td style="padding:10px">{amount:C2}</td></tr>
              </table>
              <p style="color:#888;font-size:12px">Bu e-posta otomatik gönderilmiştir, lütfen yanıtlamayınız.</p>
            </body></html>
            """;

        private static string BuildLowStockBody(string productName, int currentStock) => $"""
            <html><body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:auto">
              <h2 style="color:#dc3545">⚠️ Düşük Stok Uyarısı</h2>
              <p>Aşağıdaki ürünün stoğu kritik seviyeye düştü:</p>
              <table style="width:100%;border-collapse:collapse;margin:20px 0">
                <tr style="background:#fff3cd"><td style="padding:10px"><strong>Ürün</strong></td><td style="padding:10px">{productName}</td></tr>
                <tr><td style="padding:10px"><strong>Mevcut Stok</strong></td><td style="padding:10px;color:#dc3545"><strong>{currentStock} adet</strong></td></tr>
              </table>
              <p>Lütfen sipariş yönetim panelinizi kontrol ediniz.</p>
            </body></html>
            """;

        private static string TranslateStatus(string status) => status switch
        {
            "Confirmed" => "Onaylandı ✓",
            "Shipped"   => "Kargoya Verildi 📦",
            "Delivered" => "Teslim Edildi ✓",
            "Cancelled" => "İptal Edildi ✗",
            _           => status
        };
    }
}
