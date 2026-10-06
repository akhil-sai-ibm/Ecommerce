using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.ShippingAddressLine1).IsRequired().HasMaxLength(200);
            builder.Property(x => x.ShippingAddressLine2).HasMaxLength(200);
            builder.Property(x => x.ShippingCity).IsRequired().HasMaxLength(100);
            builder.Property(x => x.ShippingState).IsRequired().HasMaxLength(100);
            builder.Property(x => x.ShippingPostalCode).IsRequired().HasMaxLength(20);
            builder.Property(x => x.ShippingCountry).IsRequired().HasMaxLength(100);
            builder.Property(x => x.ShippingPhoneNumber).IsRequired().HasMaxLength(20);
            builder.Property(x => x.RowVersion).IsRowVersion();
        }
    }
}
