using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs.Order
{
    public class CreateOrderDto
    {
        [Required]
        public string ShippingAddressLine1 { get; set; } = string.Empty;
        public string ShippingAddressLine2 { get; set; } = string.Empty;
        [Required]
        public string ShippingCity { get; set; } = string.Empty;
        [Required]
        public string ShippingState { get; set; } = string.Empty;
        [Required]
        public string ShippingPostalCode { get; set; } = string.Empty;
        [Required]
        public string ShippingCountry { get; set; } = string.Empty;
        [Required]
        public string ShippingPhoneNumber { get; set; } = string.Empty;
    }
}
