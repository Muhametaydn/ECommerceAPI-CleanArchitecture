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
        public DbSet<User> Users => Set<User>();
        public DbSet<Address> Address => Set<Address>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
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
                    CreatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow
                }
            );
        }

        /// <summary>
        /// SaveChanges pipeline:
        /// 1) Audit alanlarını doldur (CreatedAt / UpdateAt)
        /// 2) Entity'lerden domain event'leri topla
        /// 3) DB'ye kaydet (business data + outbox mesajları tek transaction'da)
        /// 4) Domain event'leri MediatR ile dispatch et (in-process, outbox handler'ları buradan çalışır)
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

            // Entity'lerin event listelerini temizle
            entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

            // ── 3) DB'ye kaydet ───────────────────────────────────────────────
            var result = await base.SaveChangesAsync(cancellationToken);

            // ── 4) Domain event'leri dispatch et (SaveChanges sonrası) ────────
            // Handler'lar outbox tablosuna yazar; ayrı bir SaveChanges çağrısı
            // OutboxRepository.AddAsync içinde yoktur, handler'dan sonra kayıt gerekir.
            if (_domainEventDispatcher is not null && domainEvents.Count > 0)
            {
                await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

                // Outbox mesajlarını persist et
                await base.SaveChangesAsync(cancellationToken);
            }

            return result;
        }
    }
}
