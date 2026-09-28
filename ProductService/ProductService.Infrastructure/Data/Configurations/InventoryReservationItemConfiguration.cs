using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Data.Configurations
{
    public class InventoryReservationItemConfiguration : IEntityTypeConfiguration<InventoryReservationItem>
    {
        public void Configure(EntityTypeBuilder<InventoryReservationItem> builder)
        {
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.ProductId)
                   .IsRequired();

            builder.Property(x => x.Quantity)
                   .IsRequired();
        }
    }
}
