

using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Application.DTOs;

public class CalculateDiscountDto
{
    [Required]
    public int CustomerId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Order total must be greater than 0")]
    public decimal OrderTotal { get; set; }
}