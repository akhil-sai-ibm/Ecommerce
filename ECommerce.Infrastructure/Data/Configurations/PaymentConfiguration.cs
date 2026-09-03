using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).ValueGeneratedNever();

            builder.Property(p => p.Amount).IsRequired().HasColumnType("decimal(18,2)");
            builder.Property(p => p.Status).IsRequired();
            builder.Property(p => p.Method).IsRequired();
            builder.Property(p => p.TransactionId).IsRequired(false).HasMaxLength(200);

            builder.Property(p => p.CreatedAt).IsRequired().HasColumnType("datetime2");
            builder.Property(p => p.UpdatedAt).IsRequired(false).HasColumnType("datetime2");

            builder.HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<Payment>(p => p.OrderId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(p => p.OrderId).IsUnique();
        }
    }
}
