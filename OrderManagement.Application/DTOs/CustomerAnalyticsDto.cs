namespace OrderManagement.Application.DTOs;

public class CustomerAnalyticsDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerSegment { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
}