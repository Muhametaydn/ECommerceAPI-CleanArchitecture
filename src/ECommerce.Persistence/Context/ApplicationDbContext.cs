using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Outbox;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Context
{
    // IdentityDbContext zaten DbContext'ten türüyor, ayrıca DbContext ekleme!
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        private readonly IDomainEventDispatcher? _domainEventDispatcher;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IDomainEventDispatcher? domainEventDispatcher = null)
            : base(options)
        {
            _domainEventDispatcher = domainEventDispatcher;
        }

        // ── DbSets ───────────────────────────────────────────────────────────
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Address> Address => Set<Address>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<MarketplaceSettings> MarketplaceSettings => Set<MarketplaceSettings>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurations klasöründeki tüm konfigürasyonları otomatik uygular
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                    Name = "Elektronik",
                    Description = "Elektronik ürünler",
                    Slug = "elektronik",
                    IsActive = true,
                    Depth = 1,
                    SortOrder = 0,
                    CreatedAt = new DateTime(2026, 7, 16, 21, 17, 32, 974, DateTimeKind.Utc).AddTicks(2404)
                },
                new Category
                {
                    Id = Guid.Parse("b2222222-2222-2222-2222-222222222222"),
                    Name = "Giyim",
                    Description = "Giyim ürünleri",
                    Slug = "giyim",
                    IsActive = true,
                    Depth = 1,
                    SortOrder = 1,
                    CreatedAt = new DateTime(2026, 7, 16, 21, 17, 32, 974, DateTimeKind.Utc).AddTicks(2411)
                },
                new Category
                {
                    Id = Guid.Parse("c3333333-3333-3333-3333-333333333333"),
                    Name = "Ev & Yaşam",
                    Description = "Ev ve yaşam ürünleri",
                    Slug = "ev-yasam",
                    IsActive = true,
                    Depth = 1,
                    SortOrder = 2,
                    CreatedAt = new DateTime(2026, 7, 16, 21, 17, 32, 974, DateTimeKind.Utc).AddTicks(2416)
                }
            );
        }

        /// <summary>
        /// SaveChanges pipeline:
        /// 1) Audit alanlarını doldur (CreatedAt / UpdateAt)
        /// 2) Entity'lerden domain event'leri topla
        /// 3) İş verisini kaydet, domain event'leri dispatch et, outbox'ı kaydet ve birlikte commit et
        /// 4) Domain event listesini başarılı kayıttan sonra temizle
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // ── 1) Audit alanları ─────────────────────────────────────────────
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdateAt = DateTime.UtcNow;
                        break;
                }
            }

            // ── 2) Domain event'leri topla (kaydetmeden önce) ─────────────────
            var entitiesWithEvents = ChangeTracker
                .Entries<BaseEntity>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();

            var domainEvents = entitiesWithEvents
                .SelectMany(e => e.DomainEvents)
                .ToList();

            // Preserve post-save domain dispatch while committing business data and
            // outbox together. A failed scope must be discarded before retrying.
            var needsOutbox = _domainEventDispatcher is not null && domainEvents.Count > 0;
            var externalTransaction = Database.IsRelational() ? Database.CurrentTransaction : null;
            await using var ownedTransaction = needsOutbox && Database.IsRelational() && externalTransaction is null
                ? await Database.BeginTransactionAsync(cancellationToken) : null;
            var savepoint = needsOutbox && externalTransaction is not null ? "outbox_" + Guid.NewGuid().ToString("N") : null;
            if (savepoint is not null)
                await externalTransaction!.CreateSavepointAsync(savepoint, cancellationToken);
            try
            {
                var result = await base.SaveChangesAsync(cancellationToken);
                if (needsOutbox)
                {
                    await _domainEventDispatcher!.DispatchAsync(domainEvents, cancellationToken);
                    await base.SaveChangesAsync(cancellationToken);
                }
                if (ownedTransaction is not null) await ownedTransaction.CommitAsync(cancellationToken);
                if (savepoint is not null) await externalTransaction!.ReleaseSavepointAsync(savepoint, cancellationToken);
                entitiesWithEvents.ForEach(e => e.ClearDomainEvents());
                return result;
            }
            catch
            {
                if (ownedTransaction is not null) await ownedTransaction.RollbackAsync(CancellationToken.None);
                if (savepoint is not null) await externalTransaction!.RollbackToSavepointAsync(savepoint, CancellationToken.None);
                throw;
            }
        }
    }
}
