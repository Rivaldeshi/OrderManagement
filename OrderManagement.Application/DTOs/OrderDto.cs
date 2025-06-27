using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }
    public string? ShippingAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public CustomerDto? Customer { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = new();
    
    // Computed properties
    public decimal SubTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalItemsCount { get; set; }
    public int UniqueProductsCount { get; set; }
    public bool HasDiscount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public bool CanBeCancelled { get; set; }
    public bool CanBeShipped { get; set; }
    public bool CanBeDelivered { get; set; }
    public TimeSpan? ProcessingTime { get; set; }
    public TimeSpan? DeliveryTime { get; set; }
    public TimeSpan? TotalFulfillmentTime { get; set; }
}