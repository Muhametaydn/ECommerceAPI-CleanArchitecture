using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Persistence.Configurations
{
    public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
    {
        public void Configure(EntityTypeBuilder<Coupon> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(c => c.Code)
                .IsUnique();

            builder.Property(c => c.DiscountValue)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.MinimumOrderAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.DiscountType)
                .IsRequired()
                .HasConversion<int>();
        }
    }
}
