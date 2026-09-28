using System;
using System.Collections.Generic;
using ProductService.Domain.Enums;

namespace ProductService.Domain.Entities
{
    public class InventoryReservation
    {
        public Guid Id { get; set; }
        public Guid ReservationId { get; set; }
        public ReservationStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<InventoryReservationItem> Items { get; set; } = new List<InventoryReservationItem>();
    }
}
