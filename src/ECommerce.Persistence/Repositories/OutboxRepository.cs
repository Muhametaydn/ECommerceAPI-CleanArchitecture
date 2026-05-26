using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Outbox;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    public sealed class OutboxRepository : IOutboxRepository
    {
        private readonly ApplicationDbContext _context;

        public OutboxRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            await _context.OutboxMessages.AddAsync(message, cancellationToken);
            // SaveChanges çağrılmıyor — çağıran transaction içinde kaydedilir
        }

        public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(
            int batchSize = 50,
            CancellationToken cancellationToken = default)
        {
            return await _context.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            var message = await _context.OutboxMessages.FindAsync([messageId], cancellationToken);
            if (message is null) return;

            message.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
        {
            var message = await _context.OutboxMessages.FindAsync([messageId], cancellationToken);
            if (message is null) return;

            message.Error = error;
            message.RetryCount++;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
