using ECommerce.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Payload)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.ProcessedAt)
                .IsRequired(false);

            builder.Property(x => x.Error)
                .IsRequired(false)
                .HasMaxLength(2000);

            builder.Property(x => x.RetryCount)
                .HasDefaultValue(0);

            // İşlenmemiş mesajları hızlı bulmak için index
            builder.HasIndex(x => x.ProcessedAt)
                .HasDatabaseName("IX_OutboxMessages_ProcessedAt");

            // Oluşturma zamanına göre sıralı okuma için index
            builder.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("IX_OutboxMessages_CreatedAt");

            // Ignore computed property
            builder.Ignore(x => x.IsProcessed);
        }
    }
}
