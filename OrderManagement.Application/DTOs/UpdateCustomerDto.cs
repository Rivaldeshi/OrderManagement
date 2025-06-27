using System.ComponentModel.DataAnnotations;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.DTOs;
public class UpdateCustomerDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public CustomerSegment Segment { get; set; }
}