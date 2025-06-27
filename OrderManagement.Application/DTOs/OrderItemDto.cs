namespace OrderManagement.Application.DTOs;

public class OrderItemDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ProductDto? Product { get; set; }
    
    // Computed properties
    public decimal LineSubTotal { get; set; }
    public decimal LineTotal { get; set; }
    public bool HasDiscount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal EffectiveUnitPrice { get; set; }
    public decimal TotalSavings { get; set; }
}