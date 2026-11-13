using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs.Order
{
    /// <summary>
    /// Checkout request payload containing shipping destination information. Cart items are converted automatically.
    /// </summary>
    public class CreateOrderDto
    {
        /// <summary>
        /// Street address line 1.
        /// </summary>
        [Required]
        public string ShippingAddressLine1 { get; set; } = string.Empty;

        /// <summary>
        /// Street address line 2 (optional apartment, suite, unit).
        /// </summary>
        public string ShippingAddressLine2 { get; set; } = string.Empty;

        /// <summary>
        /// City.
        /// </summary>
        [Required]
        public string ShippingCity { get; set; } = string.Empty;

        /// <summary>
        /// State or province.
        /// </summary>
        [Required]
        public string ShippingState { get; set; } = string.Empty;

        /// <summary>
        /// Postal / ZIP code.
        /// </summary>
        [Required]
        public string ShippingPostalCode { get; set; } = string.Empty;

        /// <summary>
        /// Country.
        /// </summary>
        [Required]
        public string ShippingCountry { get; set; } = string.Empty;

        /// <summary>
        /// Recipient phone number for delivery updates.
        /// </summary>
        [Required]
        public string ShippingPhoneNumber { get; set; } = string.Empty;
    }
}
