using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.Status).IsConcurrencyToken();

        builder.Property(oi => oi.UnitPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Ignore(oi => oi.TotalPrice);

        builder.Property(oi => oi.ProductId)
            .IsRequired();

        // Ürün silinince geçmiş sipariş kalemleri ASLA silinmemeli (eskiden Cascade idi:
        // ürün silmek, o ürünü içeren tüm siparişlerden kalemleri yok ediyordu).
        builder.HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(oi => oi.Status)
            .IsRequired()
            .HasConversion<int>();
    }
}
