using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Data.Configurations
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.EventType).IsRequired().HasMaxLength(255);
            builder.Property(o => o.Payload).IsRequired();
            builder.HasIndex(o => o.Processed);
        }
    }
}
