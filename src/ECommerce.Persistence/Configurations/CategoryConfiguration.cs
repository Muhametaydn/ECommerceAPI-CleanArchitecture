using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Description)
                .HasMaxLength(500);

            builder.Property(c => c.Slug)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(c => c.Slug)
                .IsUnique();

            builder.Property(c => c.IsActive)
                .HasDefaultValue(true);

            builder.Property(c => c.Depth)
                .HasDefaultValue(1);

            builder.Property(c => c.SortOrder)
                .HasDefaultValue(0);

            // Hiyerarsik iliski: Parent <-> SubCategories
            builder.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Siralama ve filtreleme icin index
            builder.HasIndex(c => c.ParentCategoryId);
            builder.HasIndex(c => new { c.ParentCategoryId, c.SortOrder });
        }
    }
}
