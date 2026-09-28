using System;

namespace OrderService.Domain.Entities
{
    public class IdempotencyRecord
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string RequestPath { get; set; } = string.Empty;
        public Guid? OrderId { get; set; }
        public bool Succeeded { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
