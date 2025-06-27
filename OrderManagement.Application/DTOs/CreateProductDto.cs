using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Application.DTOs;

public class CreateProductDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
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
}