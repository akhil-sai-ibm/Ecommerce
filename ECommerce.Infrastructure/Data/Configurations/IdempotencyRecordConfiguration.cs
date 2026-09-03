using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Data.Configurations
{
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.HasKey(ir => ir.Id);
            builder.Property(ir => ir.Id).ValueGeneratedNever();

            builder.Property(ir => ir.Key).IsRequired().HasMaxLength(128);
            builder.Property(ir => ir.UserId).IsRequired();
            builder.Property(ir => ir.OrderId).IsRequired(false);
            builder.Property(ir => ir.Succeeded).IsRequired();
            builder.Property(ir => ir.ErrorMessage).IsRequired(false).HasMaxLength(500);
            builder.Property(ir => ir.ResponseStatusCode).IsRequired();
            builder.Property(ir => ir.Method).IsRequired().HasMaxLength(10);
            builder.Property(ir => ir.Endpoint).IsRequired().HasMaxLength(256);

            builder.Property(ir => ir.CreatedAt).IsRequired().HasColumnType("datetime2");
            builder.Property(ir => ir.UpdatedAt).IsRequired(false).HasColumnType("datetime2");

            // Unique constraint: (UserId, Key) pair must be unique
            // This ensures the same customer using the same key can only create one idempotency record
            builder.HasIndex(ir => new { ir.UserId, ir.Key }).IsUnique().HasName("IX_IdempotencyRecord_UserIdKey");
        }
    }
}
