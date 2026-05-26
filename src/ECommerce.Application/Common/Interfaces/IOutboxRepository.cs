using ECommerce.Domain.Outbox;

namespace ECommerce.Application.Common.Interfaces
{
    /// <summary>
    /// Outbox mesajı repository arayüzü.
    /// Domain event handler'ları bu arayüzü kullanarak outbox tablosuna yazar.
    /// Hangfire background job'u da bu arayüzü kullanarak işlenmemiş mesajları okur.
    /// </summary>
    public interface IOutboxRepository
    {
        /// <summary>Yeni bir outbox mesajı ekler (SaveChanges çağrılmaz — mevcut transaction içinde çalışır)</summary>
        Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

        /// <summary>İşlenmemiş mesajları getirir (ProcessedAt == null)</summary>
        Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize = 50, CancellationToken cancellationToken = default);

        /// <summary>Mesajı işlenmiş olarak işaretler</summary>
        Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

        /// <summary>Mesajı hatalı olarak işaretler ve retry count'u artırır</summary>
        Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
    }
}
