
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Application.DTOs;

public class UpdateProductStockDto
{
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
    public int NewStock { get; set; }
}