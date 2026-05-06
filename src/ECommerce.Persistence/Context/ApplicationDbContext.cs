using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Context
{
    // IdentityDbContext zaten DbContext'ten türüyor, ayrıca DbContext ekleme!
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Configurations klasorundeki tum konfigurasyonlari otomatik uygular
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

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) 
        {
            foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>()) 
            {
                switch (entry.State) 
                { 
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdateAt = DateTime.UtcNow;
                        break ;
                
                }
            }
        return base.SaveChangesAsync(cancellationToken);
        }
    }
}
