using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).ValueGeneratedNever();

            builder.Property(o => o.Status).IsRequired();
            builder.Property(o => o.TotalAmount).IsRequired().HasColumnType("decimal(18,2)");

            builder.Property(o => o.ShippingAddressLine1).IsRequired().HasMaxLength(200);
            builder.Property(o => o.ShippingAddressLine2).IsRequired(false).HasMaxLength(200);
            builder.Property(o => o.ShippingCity).IsRequired().HasMaxLength(100);
            builder.Property(o => o.ShippingState).IsRequired().HasMaxLength(100);
            builder.Property(o => o.ShippingPostalCode).IsRequired().HasMaxLength(50);
            builder.Property(o => o.ShippingCountry).IsRequired().HasMaxLength(100);
            builder.Property(o => o.ShippingPhoneNumber).IsRequired().HasMaxLength(50);

            builder.Property(o => o.CreatedAt).IsRequired().HasColumnType("datetime2");
            builder.Property(o => o.UpdatedAt).IsRequired(false).HasColumnType("datetime2");

            builder.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasMany(o => o.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
