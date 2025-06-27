using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagement.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;

    // Computed properties
    public decimal LineSubTotal => Quantity * UnitPrice;
    
    public decimal LineTotal => LineSubTotal - DiscountAmount;
    
    public bool HasDiscount => DiscountAmount > 0;
    
    public decimal DiscountPercentage => LineSubTotal > 0 ? (DiscountAmount / LineSubTotal) * 100 : 0;
    
    public decimal EffectiveUnitPrice => Quantity > 0 ? LineTotal / Quantity : 0;
    
    public decimal TotalSavings => DiscountAmount;
}