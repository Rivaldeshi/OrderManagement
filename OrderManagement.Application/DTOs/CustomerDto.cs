using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.DTOs;

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CustomerSegment Segment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Computed properties
    public int TotalOrdersCount { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public bool IsNewCustomer { get; set; }
    public bool IsFrequentCustomer { get; set; }
}