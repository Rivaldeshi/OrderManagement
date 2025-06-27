using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class Order
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Required]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public DateTime? ShippedDate { get; set; }
    
    public DateTime? DeliveredDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; } = 0;

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(200)]
    public string? ShippingAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Computed properties
    public decimal SubTotal => OrderItems?.Sum(oi => oi.LineTotal) ?? 0;
    
    public decimal TotalAmount => SubTotal - DiscountAmount;
    
    public int TotalItemsCount => OrderItems?.Sum(oi => oi.Quantity) ?? 0;
    
    public int UniqueProductsCount => OrderItems?.Count ?? 0;
    
    public bool HasDiscount => DiscountAmount > 0;
    
    public decimal DiscountPercentage => SubTotal > 0 ? (DiscountAmount / SubTotal) * 100 : 0;
    
    public string StatusDisplay => Status.ToString();
    
    public bool CanBeCancelled => Status == OrderStatus.Pending;
    
    public bool CanBeShipped => Status == OrderStatus.Pending;
    
    public bool CanBeDelivered => Status == OrderStatus.Shipped;
    
    public TimeSpan? ProcessingTime => ShippedDate.HasValue ? ShippedDate.Value - OrderDate : null;
    
    public TimeSpan? DeliveryTime => DeliveredDate.HasValue && ShippedDate.HasValue ? 
                                   DeliveredDate.Value - ShippedDate.Value : null;
    
    public TimeSpan? TotalFulfillmentTime => DeliveredDate.HasValue ? 
                                           DeliveredDate.Value - OrderDate : null;
}