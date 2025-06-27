using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Models;

/// <summary>
/// Detailed result of discount calculation for transparency
/// </summary>
public class DiscountCalculationResult
{
    public decimal OrderTotal { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal TotalDiscountPercentage { get; set; }

    // Breakdown of discounts
    public DiscountBreakdown Breakdown { get; set; } = new();

    public bool HasAnyDiscount => TotalDiscountAmount > 0;
}

public class DiscountBreakdown
{
    public CustomerSegmentDiscount SegmentDiscount { get; set; } = new();
    public LoyaltyDiscount LoyaltyDiscount { get; set; } = new();
    public VolumeDiscount VolumeDiscount { get; set; } = new();
    public bool MaxDiscountCapApplied { get; set; }
}

public class CustomerSegmentDiscount
{
    public CustomerSegment Segment { get; set; }
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class LoyaltyDiscount
{
    public int OrderCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public bool Qualified { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class VolumeDiscount
{
    public decimal OrderTotal { get; set; }
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public bool Qualified { get; set; }
    public string Description { get; set; } = string.Empty;
}