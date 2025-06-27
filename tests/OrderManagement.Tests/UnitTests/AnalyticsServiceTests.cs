using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using OrderManagement.Application.Services;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Interfaces;
using Xunit;

namespace OrderManagement.Tests.UnitTests;

public class AnalyticsServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<AnalyticsService>> _mockLogger;
    private readonly AnalyticsService _analyticsService;

    public AnalyticsServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<AnalyticsService>>();

        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockOrderRepository.Object);
        _mockUnitOfWork.Setup(x => x.Customers).Returns(_mockCustomerRepository.Object);

        _analyticsService = new AnalyticsService(_mockUnitOfWork.Object, _memoryCache, _mockLogger.Object);
    }

    [Fact]
    public async Task GetOrderAnalyticsAsync_WithOrders_ShouldCalculateCorrectAnalytics()
    {
        // Arrange
        var customers = new List<Customer>
        {
            new() { Id = 1, Name = "Customer 1", Segment = CustomerSegment.Premium },
            new() { Id = 2, Name = "Customer 2", Segment = CustomerSegment.Standard }
        };

        var orders = new List<Order>
        {
            new()
            {
                Id = 1,
                CustomerId = 1,
                Status = OrderStatus.Delivered,
                OrderDate = DateTime.UtcNow.AddDays(-10),
                DeliveredDate = DateTime.UtcNow.AddDays(-8),
                DiscountAmount = 20m,
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = 1, Product = new Product { Name = "Laptop" }, Quantity = 1, UnitPrice = 1000m }
                }
            },
            new()
            {
                Id = 2,
                CustomerId = 2,
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow.AddDays(-2),
                DiscountAmount = 10m,
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = 2, Product = new Product { Name = "Mouse" }, Quantity = 2, UnitPrice = 50m }
                }
            }
        };

        _mockOrderRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(orders);
        _mockCustomerRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(customers);

        // Act
        var analytics = await _analyticsService.GetOrderAnalyticsAsync();

        // Assert
        analytics.Should().NotBeNull();
        analytics.TotalOrders.Should().Be(2);
        analytics.TotalCustomers.Should().Be(2);
        analytics.TotalRevenue.Should().Be(1070m); // (1000-20) + (100-10)
        analytics.AverageOrderValue.Should().Be(535m); // 1070/2
        analytics.TotalDiscountsGiven.Should().Be(30m);
        analytics.IsFromCache.Should().BeFalse();

        // Status breakdown
        analytics.StatusBreakdown.PendingOrders.Should().Be(1);
        analytics.StatusBreakdown.DeliveredOrders.Should().Be(1);
        analytics.StatusBreakdown.PendingPercentage.Should().Be(50m);
        analytics.StatusBreakdown.DeliveredPercentage.Should().Be(50m);

        // Top selling product
        analytics.TopSellingProduct.Should().NotBeNull();
        analytics.TopSellingProduct!.ProductName.Should().Be("Mouse");
        analytics.TopSellingProduct.TotalQuantitySold.Should().Be(2);
    }

    [Fact]
    public async Task GetOrderAnalyticsAsync_NoOrders_ShouldReturnEmptyAnalytics()
    {
        // Arrange
        _mockOrderRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Order>());
        _mockCustomerRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Customer>());

        // Act
        var analytics = await _analyticsService.GetOrderAnalyticsAsync();

        // Assert
        analytics.Should().NotBeNull();
        analytics.TotalOrders.Should().Be(0);
        analytics.TotalCustomers.Should().Be(0);
        analytics.TotalRevenue.Should().Be(0);
        analytics.AverageOrderValue.Should().Be(0);
        analytics.TopSellingProduct.Should().BeNull();
        analytics.TopCustomer.Should().BeNull();
    }

    [Fact]
    public async Task GetOrderAnalyticsAsync_WithCache_ShouldReturnCachedResult()
    {
        // Arrange
        _mockOrderRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Order>());
        _mockCustomerRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Customer>());

        // First call to populate cache
        var firstResult = await _analyticsService.GetOrderAnalyticsAsync();

        // Act - Second call should use cache
        var secondResult = await _analyticsService.GetOrderAnalyticsAsync();

        // Assert
        secondResult.IsFromCache.Should().BeTrue();
        _mockOrderRepository.Verify(x => x.GetAllAsync(), Times.Once); // Should only be called once
    }

    [Fact]
    public async Task InvalidateCacheAsync_ShouldClearCache()
    {
        // Arrange
        _mockOrderRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Order>());
        _mockCustomerRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Customer>());

        // Populate cache
        await _analyticsService.GetOrderAnalyticsAsync();

        // Act
        await _analyticsService.InvalidateCacheAsync();

        // Get analytics again - should not be from cache
        var result = await _analyticsService.GetOrderAnalyticsAsync();

        // Assert
        result.IsFromCache.Should().BeFalse();
        _mockOrderRepository.Verify(x => x.GetAllAsync(), Times.Exactly(2)); // Called twice after cache invalidation
    }

    [Fact]
    public async Task GetAnalyticsForPeriodAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow.AddDays(-1);

        var orders = new List<Order>
        {
            new() 
            { 
                OrderDate = DateTime.UtcNow.AddDays(-20), // Within range
                DiscountAmount = 5m,
                OrderItems = new List<OrderItem> 
                { 
                    new() { Quantity = 1, UnitPrice = 100m } // TotalAmount = 95
                }
            },
            new() 
            { 
                OrderDate = DateTime.UtcNow.AddDays(-40), // Outside range
                DiscountAmount = 10m,
                OrderItems = new List<OrderItem> 
                { 
                    new() { Quantity = 1, UnitPrice = 200m } // TotalAmount = 190
                }
            }
        };

        _mockOrderRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(orders);
        _mockCustomerRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Customer>());

        // Act
        var analytics = await _analyticsService.GetAnalyticsForPeriodAsync(startDate, endDate);

        // Assert
        analytics.TotalOrders.Should().Be(1); // Only one order in the date range
        analytics.TotalRevenue.Should().Be(95m); // Only the order within range (100-5)
    }
}
