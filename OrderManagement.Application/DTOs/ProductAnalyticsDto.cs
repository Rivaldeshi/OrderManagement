namespace OrderManagement.Application.DTOs;

public class ProductAnalyticsDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TimesOrdered { get; set; }
}
