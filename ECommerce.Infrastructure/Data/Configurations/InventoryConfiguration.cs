using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Data.Configurations
{
    public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
    {
        public void Configure(EntityTypeBuilder<Inventory> builder)
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).ValueGeneratedNever();

            builder.Property(i => i.Quantity).IsRequired();
            builder.Property(i => i.ReorderLevel).IsRequired();

            // Concurrency token: SQL Server rowversion automatically managed by EF Core
            builder.Property(i => i.RowVersion)
                .IsRowVersion()
                .IsRequired(false);

            // Prevent negative values
            builder.HasCheckConstraint("CK_Inventory_Quantity_NonNegative", "Quantity >= 0");
            builder.HasCheckConstraint("CK_Inventory_ReorderLevel_NonNegative", "ReorderLevel >= 0");

            builder.HasIndex(i => i.ProductId).IsUnique();

            builder.HasOne(i => i.Product)
                .WithOne(p => p.Inventory)
                .HasForeignKey<Inventory>(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        }
    }
}
