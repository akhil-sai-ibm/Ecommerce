using System;

namespace ProductService.Domain.Entities
{
    public class Inventory : BaseEntity
    {
        public Guid ProductId { get; set; }

        public Product? Product { get; set; }

        public int Quantity { get; set; }

        public int ReorderLevel { get; set; }

        // Concurrency token for optimistic concurrency control
        // This byte array is automatically managed by EF Core and reflects the SQL rowversion
        public byte[]? RowVersion { get; set; }

        // UpdatedAt is inherited from BaseEntity
    }
}

