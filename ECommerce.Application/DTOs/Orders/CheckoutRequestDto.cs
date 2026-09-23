using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Orders
{
    public class CheckoutRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string ShippingAddressLine1 { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ShippingAddressLine2 { get; set; }

        [Required]
        [MaxLength(100)]
        public string ShippingCity { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ShippingState { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ShippingPostalCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ShippingCountry { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ShippingPhoneNumber { get; set; } = string.Empty;
    }
}
