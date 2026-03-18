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
    public class AddressConfiguration : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder) 
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Tittle)
           .IsRequired()
           .HasMaxLength(50);

            builder.Property(a => a.AddressLine)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.District)
                .HasMaxLength(100);

            builder.Property(a => a.PostalCode)
                .HasMaxLength(10);

            builder.Property(a => a.Country)
                .IsRequired()
                .HasMaxLength(100);




        }
    }
}
