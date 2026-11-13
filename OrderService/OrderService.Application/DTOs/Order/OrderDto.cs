using System;
using System.Collections.Generic;

namespace OrderService.Application.DTOs.Order
{
    /// <summary>
    /// Order details returned for customer order history and administrative order review.
    /// </summary>
    public class OrderDto
    {
        /// <summary>
        /// Unique order identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the customer who placed the order.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Current numeric status of the order (0: Pending, 1: Confirmed, 2: Processing, 3: Shipped, 4: Delivered, 5: Cancelled).
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Total monetary amount for the order.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Order creation timestamp in UTC.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Street address line 1.
        /// </summary>
        public string ShippingAddressLine1 { get; set; } = string.Empty;

        /// <summary>
        /// Street address line 2, apartment or suite.
        /// </summary>
        public string ShippingAddressLine2 { get; set; } = string.Empty;

        /// <summary>
        /// Shipping city.
        /// </summary>
        public string ShippingCity { get; set; } = string.Empty;

        /// <summary>
        /// Shipping state, province, or region.
        /// </summary>
        public string ShippingState { get; set; } = string.Empty;

        /// <summary>
        /// Postal / ZIP code.
        /// </summary>
        public string ShippingPostalCode { get; set; } = string.Empty;

        /// <summary>
        /// Destination country.
        /// </summary>
        public string ShippingCountry { get; set; } = string.Empty;

        /// <summary>
        /// Recipient contact phone number.
        /// </summary>
        public string ShippingPhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Line items included in the order.
        /// </summary>
        public IEnumerable<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
