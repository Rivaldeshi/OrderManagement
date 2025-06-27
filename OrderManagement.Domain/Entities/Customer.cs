using System.ComponentModel.DataAnnotations;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Entities;

public class Customer
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public CustomerSegment Segment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    // Computed properties
    public int TotalOrdersCount => Orders?.Count ?? 0;
    
    public decimal TotalSpent => Orders?.Sum(o => o.TotalAmount) ?? 0;
    
    public DateTime? LastOrderDate => Orders?.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate;
    
    public bool IsNewCustomer => TotalOrdersCount == 0;
    
    public bool IsFrequentCustomer => TotalOrdersCount > 5;
}