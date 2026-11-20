using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(p => p.StockQuantity).IsConcurrencyToken();

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .HasMaxLength(2000);

            builder.Property(p => p.Price)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.SKU)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.ImageUrl)
                .HasMaxLength(500);

            builder.Property(p => p.ApprovalStatus)
                .HasDefaultValue(ECommerce.Domain.Enums.ProductApprovalStatus.Approved)
                // Always persist the domain status, including Pending (the enum zero value).
                .ValueGeneratedNever();

            // ── Indexler ─────────────────────────────────────────────────────
            // SKU benzersiz arama
            builder.HasIndex(p => p.SKU)
                .IsUnique();

            // Kategori bazlı filtreleme (en sık kullanılan filtre)
            builder.HasIndex(p => p.CategoryId);

            // Aktif ürün listesi + fiyat sıralaması (composite)
            builder.HasIndex(p => new { p.IsActive, p.Price });

            // Aktif ürün listesi + tarih sıralaması (varsayılan sıralama)
            builder.HasIndex(p => new { p.IsActive, p.CreatedAt });

            // Aktif ürün + kategori + fiyat aralığı filtresi (yaygın sorgu kombinasyonu)
            builder.HasIndex(p => new { p.CategoryId, p.IsActive, p.Price });

            builder.HasIndex(p => new { p.SellerId, p.ApprovalStatus });

            // ── İlişkiler ─────────────────────────────────────────────────────
            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Seller)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Reviews)
                .WithOne(r => r.Product)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
