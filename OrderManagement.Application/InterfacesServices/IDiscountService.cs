using OrderManagement.Application.Models;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Services;

public interface IDiscountService
{
    /// <summary>
    /// Calculates the total discount amount for a customer's order
    /// </summary>
    /// <param name="customer">The customer placing the order</param>
    /// <param name="orderTotal">The total amount of the order before discount</param>
    /// <returns>The discount amount to be applied</returns>
    Task<decimal> CalculateDiscountAsync(Customer customer, decimal orderTotal);
    
    /// <summary>
    /// Calculates detailed discount breakdown for transparency
    /// </summary>
    /// <param name="customer">The customer placing the order</param>
    /// <param name="orderTotal">The total amount of the order before discount</param>
    /// <returns>Detailed discount calculation result</returns>
    Task<DiscountCalculationResult> CalculateDetailedDiscountAsync(Customer customer, decimal orderTotal);
    
    /// <summary>
    /// Gets the discount percentage for a customer segment
    /// </summary>
    /// <param name="segment">Customer segment</param>
    /// <returns>Discount percentage (0.0 to 1.0)</returns>
    decimal GetSegmentDiscountPercentage(CustomerSegment segment);
    
    /// <summary>
    /// Gets the loyalty discount percentage based on order history
    /// </summary>
    /// <param name="orderCount">Number of previous orders</param>
    /// <returns>Loyalty discount percentage (0.0 to 1.0)</returns>
    decimal GetLoyaltyDiscountPercentage(int orderCount);
    
    /// <summary>
    /// Checks if customer qualifies for volume discount
    /// </summary>
    /// <param name="orderTotal">Order total amount</param>
    /// <returns>Volume discount percentage (0.0 to 1.0)</returns>
    decimal GetVolumeDiscountPercentage(decimal orderTotal);
}