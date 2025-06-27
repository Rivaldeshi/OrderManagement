using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Application.DTOs;

public class CreateOrderDto
{
    [Required]
    public int CustomerId { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    [StringLength(200)]
    public string? ShippingAddress { get; set; }
    
    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item")]
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}