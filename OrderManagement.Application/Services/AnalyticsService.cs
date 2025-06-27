using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.DTOs;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Interfaces;

namespace OrderManagement.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalyticsService> _logger;

    // Cache configuration
    private const string ANALYTICS_CACHE_KEY = "order_analytics";
    private const int CACHE_DURATION_MINUTES = 15; // Cache for 15 minutes
    private const string LAST_MODIFIED_CACHE_KEY = "orders_last_modified";

    public AnalyticsService(
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        ILogger<AnalyticsService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<OrderAnalyticsDto> GetOrderAnalyticsAsync(bool forceRefresh = false)
    {
        var cacheKey = ANALYTICS_CACHE_KEY;

        // Check if we should use cached data
        if (!forceRefresh && await ShouldUseCachedDataAsync())
        {
            if (_cache.TryGetValue(cacheKey, out OrderAnalyticsDto? cachedAnalytics) && cachedAnalytics != null)
            {
                _logger.LogInformation("Returning cached analytics data");
                cachedAnalytics.IsFromCache = true;
                return cachedAnalytics;
            }
        }

        _logger.LogInformation("Calculating fresh analytics data");

        // Calculate fresh analytics
        var analytics = await CalculateAnalyticsAsync();
        analytics.IsFromCache = false;

        // Cache the result
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES),
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, analytics, cacheOptions);

        // Update last calculated timestamp
        _cache.Set(LAST_MODIFIED_CACHE_KEY, DateTime.UtcNow, TimeSpan.FromHours(24));

        _logger.LogInformation("Analytics cached for {Duration} minutes", CACHE_DURATION_MINUTES);

        return analytics;
    }

    public async Task InvalidateCacheAsync()
    {
        _cache.Remove(ANALYTICS_CACHE_KEY);
        _cache.Set(LAST_MODIFIED_CACHE_KEY, DateTime.UtcNow, TimeSpan.FromHours(24));

        _logger.LogInformation("Analytics cache invalidated");
        await Task.CompletedTask;
    }

    public async Task<OrderAnalyticsDto> GetAnalyticsForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"analytics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out OrderAnalyticsDto? cachedPeriodAnalytics) && cachedPeriodAnalytics != null)
        {
            cachedPeriodAnalytics.IsFromCache = true;
            return cachedPeriodAnalytics;
        }

        var analytics = await CalculateAnalyticsAsync(startDate, endDate);
        analytics.IsFromCache = false;

        // Cache period analytics for longer (they don't change once the period is over)
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = endDate < DateTime.UtcNow.Date
                ? TimeSpan.FromHours(24) // Past periods cache longer
                : TimeSpan.FromMinutes(30), // Current periods cache shorter
            Priority = CacheItemPriority.Low
        };

        _cache.Set(cacheKey, analytics, cacheOptions);

        return analytics;
    }

    #region Private Methods

    private async Task<bool> ShouldUseCachedDataAsync()
    {
        // Simple logic: if no orders have been modified recently, use cache
        if (_cache.TryGetValue(LAST_MODIFIED_CACHE_KEY, out DateTime lastModified))
        {
            // If cache is less than 5 minutes old, definitely use it
            return DateTime.UtcNow - lastModified < TimeSpan.FromMinutes(5);
        }

        return false;
    }

    private async Task<OrderAnalyticsDto> CalculateAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Get all orders with navigation properties
        var allOrders = (await _unitOfWork.Orders.GetAllAsync()).ToList();
        var allCustomers = (await _unitOfWork.Customers.GetAllAsync()).ToList();

        // Filter by date range if specified
        var orders = startDate.HasValue || endDate.HasValue
            ? allOrders.Where(o =>
                (!startDate.HasValue || o.OrderDate >= startDate.Value) &&
                (!endDate.HasValue || o.OrderDate <= endDate.Value)).ToList()
            : allOrders;

        var analytics = new OrderAnalyticsDto
        {
            LastUpdated = DateTime.UtcNow,
            TotalOrders = orders.Count,
            TotalCustomers = allCustomers.Count
        };

        if (orders.Any())
        {
            // Basic metrics
            analytics.TotalRevenue = orders.Sum(o => o.TotalAmount);
            analytics.AverageOrderValue = analytics.TotalRevenue / orders.Count;
            analytics.TotalDiscountsGiven = orders.Sum(o => o.DiscountAmount);
            analytics.AverageDiscountPercentage = orders.Where(o => o.SubTotal > 0)
                .Average(o => o.DiscountPercentage);
 
            // Items per order
            analytics.AverageItemsPerOrder = orders.Average(o => o.TotalItemsCount);

            // Fulfillment time calculation
            var deliveredOrders = orders.Where(o =>
                o.Status == OrderStatus.Delivered &&
                o.DeliveredDate.HasValue).ToList();

            if (deliveredOrders.Any())
            {
                var fulfillmentTimes = deliveredOrders
                    .Select(o => o.TotalFulfillmentTime)
                    .Where(t => t.HasValue)
                    .Select(t => t!.Value)
                    .ToList();

                if (fulfillmentTimes.Any())
                {
                    var averageTicks = (long)fulfillmentTimes.Average(t => t.Ticks);
                    analytics.AverageFulfillmentTime = new TimeSpan(averageTicks);
                    analytics.AverageFulfillmentTimeFormatted = FormatTimeSpan(analytics.AverageFulfillmentTime.Value);
                }
            }

            // Status breakdown
            analytics.StatusBreakdown = CalculateStatusBreakdown(orders);

            // Top selling product
            analytics.TopSellingProduct = CalculateTopSellingProduct(orders);

            // Top customer
            analytics.TopCustomer = CalculateTopCustomer(allCustomers);
        }

        stopwatch.Stop();
        _logger.LogInformation("Analytics calculated in {ElapsedMs}ms for {OrderCount} orders",
            stopwatch.ElapsedMilliseconds, orders.Count);

        return analytics;
    }

    private OrderStatusBreakdown CalculateStatusBreakdown(List<Order> orders)
    {
        var totalOrders = orders.Count;
        var breakdown = new OrderStatusBreakdown();

        if (totalOrders == 0) return breakdown;

        breakdown.PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);
        breakdown.ShippedOrders = orders.Count(o => o.Status == OrderStatus.Shipped);
        breakdown.DeliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered);
        breakdown.CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);

        breakdown.PendingPercentage = (decimal)breakdown.PendingOrders / totalOrders * 100;
        breakdown.ShippedPercentage = (decimal)breakdown.ShippedOrders / totalOrders * 100;
        breakdown.DeliveredPercentage = (decimal)breakdown.DeliveredOrders / totalOrders * 100;
        breakdown.CancelledPercentage = (decimal)breakdown.CancelledOrders / totalOrders * 100;

        return breakdown;
    }

    private ProductAnalyticsDto? CalculateTopSellingProduct(List<Order> orders)
    {
        var productSales = orders
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                ProductName = g.First().Product?.Name ?? "Unknown",
                TotalQuantity = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.LineTotal),
                TimesOrdered = g.Count()
            })
            .OrderByDescending(p => p.TotalQuantity)
            .FirstOrDefault();

        return productSales == null ? null : new ProductAnalyticsDto
        {
            ProductId = productSales.ProductId,
            ProductName = productSales.ProductName,
            TotalQuantitySold = productSales.TotalQuantity,
            TotalRevenue = productSales.TotalRevenue,
            TimesOrdered = productSales.TimesOrdered
        };
    }

    private CustomerAnalyticsDto? CalculateTopCustomer(List<Customer> customers)
    {
        var topCustomer = customers
            .Where(c => c.TotalSpent > 0)
            .OrderByDescending(c => c.TotalSpent)
            .FirstOrDefault();

        return topCustomer == null ? null : new CustomerAnalyticsDto
        {
            CustomerId = topCustomer.Id,
            CustomerName = topCustomer.Name,
            CustomerSegment = topCustomer.Segment.ToString(),
            TotalOrders = topCustomer.TotalOrdersCount,
            TotalSpent = topCustomer.TotalSpent,
            AverageOrderValue = topCustomer.TotalOrdersCount > 0
                ? topCustomer.TotalSpent / topCustomer.TotalOrdersCount
                : 0
        };
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
            return $"{(int)timeSpan.TotalDays} days, {timeSpan.Hours} hours";
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours} hours, {timeSpan.Minutes} minutes";
        return $"{timeSpan.Minutes} minutes";
    }

    #endregion
}