using System;

namespace ECommerce.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public Guid UserId { get; set; }

        public User? User { get; set; }

        public Guid? OrderId { get; set; }

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }
    }
}
