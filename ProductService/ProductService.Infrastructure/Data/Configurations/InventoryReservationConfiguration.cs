using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Data.Configurations
{
    public class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
    {
        public void Configure(EntityTypeBuilder<InventoryReservation> builder)
        {
            builder.HasKey(x => x.Id);
            
            builder.HasIndex(x => x.ReservationId).IsUnique();

            builder.Property(x => x.Status)
                   .IsRequired();

            builder.HasMany(x => x.Items)
                   .WithOne(x => x.Reservation)
                   .HasForeignKey(x => x.InventoryReservationId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
