using OrderManagement.Application.DTOs;

namespace OrderManagement.Application.Services;

/// <summary>
/// Service for calculating order analytics with caching support
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Gets comprehensive order analytics with caching
    /// </summary>
    /// <param name="forceRefresh">If true, bypasses cache and recalculates</param>
    /// <returns>Order analytics data</returns>
    Task<OrderAnalyticsDto> GetOrderAnalyticsAsync(bool forceRefresh = false);
    
    /// <summary>
    /// Invalidates the analytics cache (call when orders are modified)
    /// </summary>
    Task InvalidateCacheAsync();
    
    /// <summary>
    /// Gets analytics for a specific date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Analytics for the specified period</returns>
    Task<OrderAnalyticsDto> GetAnalyticsForPeriodAsync(DateTime startDate, DateTime endDate);
}