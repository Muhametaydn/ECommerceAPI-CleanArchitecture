namespace ECommerce.Application.Common.Interfaces
{
    /// <summary>
    /// E-posta gönderim servisi arayüzü.
    /// FluentEmail + MailKit ile implement edilir (Infrastructure katmanı).
    /// </summary>
    public interface IEmailService
    {
        /// <summary>Sipariş oluşturuldu onay e-postası</summary>
        Task SendOrderConfirmationAsync(string toEmail, string toName, string orderNumber, decimal totalAmount, CancellationToken ct = default);

        /// <summary>Sipariş durum değişikliği bildirimi</summary>
        Task SendOrderStatusChangedAsync(string toEmail, string toName, string orderNumber, string newStatus, CancellationToken ct = default);

        /// <summary>Ödeme tamamlandı makbuzu</summary>
        Task SendPaymentReceiptAsync(string toEmail, string toName, decimal amount, string transactionId, CancellationToken ct = default);

        /// <summary>Admin'e düşük stok uyarısı</summary>
        Task SendLowStockAlertAsync(string adminEmail, string productName, int currentStock, CancellationToken ct = default);
    }
}
