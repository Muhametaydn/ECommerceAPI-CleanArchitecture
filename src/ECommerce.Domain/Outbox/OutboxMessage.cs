namespace ECommerce.Domain.Outbox
{
    /// <summary>
    /// Outbox Pattern için mesaj kaydı.
    /// Domain event'leri asenkron olarak dağıtmak için veritabanında saklanır.
    /// Hangfire background job'u bu tabloyu okuyarak RabbitMQ'ya iletecek (Faz 4b).
    /// </summary>
    public sealed class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Event'in tam tip adı (örn: "ECommerce.Domain.Events.Orders.OrderCreatedDomainEvent")</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Event verisinin JSON serialize edilmiş hali</summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>Mesajın oluşturulma zamanı (UTC)</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Mesajın işlenme zamanı (null = henüz işlenmedi)</summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// İşlem sırasında oluşan hata mesajı (null = hata yok).
        /// Başarısız mesajlar bu alanla izlenebilir.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>Yeniden deneme sayısı</summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>Mesajın işlenip işlenmediğini kontrol eden computed property</summary>
        public bool IsProcessed => ProcessedAt.HasValue;
    }
}
