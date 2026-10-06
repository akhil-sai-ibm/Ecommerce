namespace OrderService.Application.DTOs.Payments
{
    public class PaymentDto
    {
        public System.Guid Id { get; set; }
        public System.Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public int Status { get; set; } // 1 is Successful
    }
}
