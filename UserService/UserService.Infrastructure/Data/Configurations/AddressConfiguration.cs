using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Data.Configurations
{
    public class AddressConfiguration : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).ValueGeneratedNever();

            builder.Property(a => a.AddressLine1).IsRequired().HasMaxLength(500);
            builder.Property(a => a.AddressLine2).IsRequired(false).HasMaxLength(500);
            builder.Property(a => a.City).IsRequired().HasMaxLength(200);
            builder.Property(a => a.State).IsRequired().HasMaxLength(200);
            builder.Property(a => a.PostalCode).IsRequired().HasMaxLength(50);
            builder.Property(a => a.Country).IsRequired().HasMaxLength(200);
            builder.Property(a => a.IsDefault).IsRequired();

            builder.HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        }
    }
}
