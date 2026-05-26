namespace ECommerce.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdateAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // ── Domain Events ─────────────────────────────────────────────────────
        private readonly List<IDomainEvent> _domainEvents = new();

        /// <summary>
        /// Entity'de biriken domain event'lerini dış dünyaya read-only olarak sunar.
        /// SaveChangesAsync sırasında ApplicationDbContext tarafından toplanır.
        /// </summary>
        public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>Entity içindeki iş metodlarında çağrılır (örn: Order.Confirm())</summary>
        protected void AddDomainEvent(IDomainEvent domainEvent)
            => _domainEvents.Add(domainEvent);

        /// <summary>
        /// Dispatch sonrasında event listesini temizler.
        /// ApplicationDbContext.SaveChangesAsync tarafından çağrılır.
        /// </summary>
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
