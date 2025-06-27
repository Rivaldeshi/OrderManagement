namespace OrderManagement.Application.DTOs;
public class OrderAnalyticsDto
{
    public decimal AverageOrderValue { get; set; }
    public TimeSpan? AverageFulfillmentTime { get; set; }
    public string AverageFulfillmentTimeFormatted { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int TotalCustomers { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime? LastUpdated { get; set; }
    
    // Order status breakdown
    public OrderStatusBreakdown StatusBreakdown { get; set; } = new();
    
    // Additional useful metrics
    public double AverageItemsPerOrder { get; set; }
    public decimal TotalDiscountsGiven { get; set; }
    public decimal AverageDiscountPercentage { get; set; }
    public ProductAnalyticsDto? TopSellingProduct { get; set; }
    public CustomerAnalyticsDto? TopCustomer { get; set; }
    
    // Performance metrics
    public bool IsFromCache { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
