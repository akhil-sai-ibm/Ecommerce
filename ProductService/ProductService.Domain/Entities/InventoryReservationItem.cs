using System;

namespace ProductService.Domain.Entities
{
    public class InventoryReservationItem
    {
        public Guid Id { get; set; }
        public Guid InventoryReservationId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }

        public InventoryReservation Reservation { get; set; }
    }
}
