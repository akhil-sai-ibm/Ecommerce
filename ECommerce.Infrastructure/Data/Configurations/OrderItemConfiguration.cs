using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Data.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasKey(oi => oi.Id);
            builder.Property(oi => oi.Id).ValueGeneratedNever();

            builder.Property(oi => oi.Quantity).IsRequired();
            builder.HasCheckConstraint("CK_OrderItem_Quantity_Positive", "Quantity > 0");

            builder.Property(oi => oi.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");

            builder.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        }
    }
}
