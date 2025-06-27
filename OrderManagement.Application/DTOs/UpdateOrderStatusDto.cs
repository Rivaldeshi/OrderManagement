using System.ComponentModel.DataAnnotations;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.DTOs;

public class UpdateOrderStatusDto
{
    [Required]
    public OrderStatus NewStatus { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
}