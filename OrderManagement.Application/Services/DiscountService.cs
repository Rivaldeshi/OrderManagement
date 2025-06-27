using OrderManagement.Application.Models;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Services;

public class DiscountService : IDiscountService
{
    // Discount configuration constants
    private const decimal NEW_CUSTOMER_DISCOUNT = 0.10m;        // 10% for new customers
    private const decimal STANDARD_CUSTOMER_DISCOUNT = 0.05m;   // 5% for standard customers
    private const decimal PREMIUM_CUSTOMER_DISCOUNT = 0.15m;    // 15% for premium customers

    private const decimal LOYALTY_DISCOUNT_THRESHOLD = 5;       // 5+ orders for loyalty discount
    private const decimal LOYALTY_DISCOUNT_RATE = 0.05m;       // Additional 5% for loyal customers

    private const decimal VOLUME_DISCOUNT_THRESHOLD = 500m;     // $500+ for volume discount
    private const decimal VOLUME_DISCOUNT_RATE = 0.03m;        // Additional 3% for large orders

    private const decimal MAX_TOTAL_DISCOUNT = 0.25m;          // Maximum 25% total discount

    public async Task<decimal> CalculateDiscountAsync(Customer customer, decimal orderTotal)
    {
        var result = await CalculateDetailedDiscountAsync(customer, orderTotal);
        return result.TotalDiscountAmount;
    }

    public async Task<DiscountCalculationResult> CalculateDetailedDiscountAsync(Customer customer, decimal orderTotal)
    {
        if (orderTotal <= 0)
            return new DiscountCalculationResult { OrderTotal = orderTotal };

        var result = new DiscountCalculationResult
        {
            OrderTotal = orderTotal
        };

        // Calculate segment discount
        var segmentDiscountPercentage = GetSegmentDiscountPercentage(customer.Segment);
        var segmentDiscountAmount = orderTotal * segmentDiscountPercentage;

        result.Breakdown.SegmentDiscount = new CustomerSegmentDiscount
        {
            Segment = customer.Segment,
            Percentage = segmentDiscountPercentage,
            Amount = segmentDiscountAmount,
            Description = GetSegmentDiscountDescription(customer.Segment)
        };

        // Calculate loyalty discount
        var loyaltyDiscountPercentage = GetLoyaltyDiscountPercentage(customer.TotalOrdersCount);
        var loyaltyDiscountAmount = orderTotal * loyaltyDiscountPercentage;

        result.Breakdown.LoyaltyDiscount = new LoyaltyDiscount
        {
            OrderCount = customer.TotalOrdersCount,
            Percentage = loyaltyDiscountPercentage,
            Amount = loyaltyDiscountAmount,
            Qualified = customer.TotalOrdersCount >= LOYALTY_DISCOUNT_THRESHOLD,
            Description = GetLoyaltyDiscountDescription(customer.TotalOrdersCount)
        };

        // Calculate volume discount
        var volumeDiscountPercentage = GetVolumeDiscountPercentage(orderTotal);
        var volumeDiscountAmount = orderTotal * volumeDiscountPercentage;

        result.Breakdown.VolumeDiscount = new VolumeDiscount
        {
            OrderTotal = orderTotal,
            Percentage = volumeDiscountPercentage,
            Amount = volumeDiscountAmount,
            Qualified = orderTotal >= VOLUME_DISCOUNT_THRESHOLD,
            Description = GetVolumeDiscountDescription(orderTotal)
        };

        // Sum all discounts
        var totalDiscount = segmentDiscountAmount + loyaltyDiscountAmount + volumeDiscountAmount;

        // Apply maximum discount cap
        var maxAllowedDiscount = orderTotal * MAX_TOTAL_DISCOUNT;
        if (totalDiscount > maxAllowedDiscount)
        {
            totalDiscount = maxAllowedDiscount;
            result.Breakdown.MaxDiscountCapApplied = true;
        }

        // Set final results
        result.TotalDiscountAmount = Math.Max(0, totalDiscount);
        result.FinalAmount = orderTotal - result.TotalDiscountAmount;
        result.TotalDiscountPercentage = orderTotal > 0 ? result.TotalDiscountAmount / orderTotal : 0;

        return result;
    }

    public decimal GetSegmentDiscountPercentage(CustomerSegment segment)
    {
        return segment switch
        {
            CustomerSegment.New => NEW_CUSTOMER_DISCOUNT,
            CustomerSegment.Standard => STANDARD_CUSTOMER_DISCOUNT,
            CustomerSegment.Premium => PREMIUM_CUSTOMER_DISCOUNT,
            _ => 0m
        };
    }

    public decimal GetLoyaltyDiscountPercentage(int orderCount)
    {
        return orderCount >= LOYALTY_DISCOUNT_THRESHOLD ? LOYALTY_DISCOUNT_RATE : 0m;
    }

    public decimal GetVolumeDiscountPercentage(decimal orderTotal)
    {
        return orderTotal >= VOLUME_DISCOUNT_THRESHOLD ? VOLUME_DISCOUNT_RATE : 0m;
    }

    #region Private Helper Methods

    private string GetSegmentDiscountDescription(CustomerSegment segment)
    {
        return segment switch
        {
            CustomerSegment.New => "New customer welcome discount",
            CustomerSegment.Standard => "Standard customer discount",
            CustomerSegment.Premium => "Premium customer discount",
            _ => "No segment discount"
        };
    }

    private string GetLoyaltyDiscountDescription(int orderCount)
    {
        return orderCount >= LOYALTY_DISCOUNT_THRESHOLD
            ? $"Loyal customer discount (you have {orderCount} orders)"
            : $"Loyalty discount available after {LOYALTY_DISCOUNT_THRESHOLD} orders (you have {orderCount})";
    }

    private string GetVolumeDiscountDescription(decimal orderTotal)
    {
        return orderTotal >= VOLUME_DISCOUNT_THRESHOLD
            ? $"Volume discount for orders over ${VOLUME_DISCOUNT_THRESHOLD:F2}"
            : $"Volume discount available for orders over ${VOLUME_DISCOUNT_THRESHOLD:F2}";
    }

    #endregion
}