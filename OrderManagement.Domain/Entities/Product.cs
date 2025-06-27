using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderManagement.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
    public int Stock { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    [StringLength(20)]
    public string? SKU { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }

    // Computed properties
    public bool IsInStock => Stock > 0;
    
    public bool IsLowStock => Stock > 0 && Stock <= 10;
    
    public bool IsOutOfStock => Stock == 0;
    
    public string StockStatus => IsOutOfStock ? "Out of Stock" : 
                               IsLowStock ? "Low Stock" : 
                               "In Stock";
}