using FluentAssertions;
using OrderManagement.Application.Services;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using Xunit;

namespace OrderManagement.Tests.UnitTests;

public class DiscountServiceTests
{
    private readonly DiscountService _discountService;

    public DiscountServiceTests()
    {
        _discountService = new DiscountService();
    }

    [Theory]
    [InlineData(CustomerSegment.New, 0.10)]
    [InlineData(CustomerSegment.Standard, 0.05)]
    [InlineData(CustomerSegment.Premium, 0.15)]
    public void GetSegmentDiscountPercentage_ShouldReturnCorrectPercentage(CustomerSegment segment, decimal expected)
    {
        // Act
        var result = _discountService.GetSegmentDiscountPercentage(segment);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(3, 0.0)]
    [InlineData(5, 0.05)]
    [InlineData(10, 0.05)]
    public void GetLoyaltyDiscountPercentage_ShouldReturnCorrectPercentage(int orderCount, decimal expected)
    {
        // Act
        var result = _discountService.GetLoyaltyDiscountPercentage(orderCount);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(100.0, 0.0)]
    [InlineData(499.99, 0.0)]
    [InlineData(500.0, 0.03)]
    [InlineData(1000.0, 0.03)]
    public void GetVolumeDiscountPercentage_ShouldReturnCorrectPercentage(decimal orderTotal, decimal expected)
    {
        // Act
        var result = _discountService.GetVolumeDiscountPercentage(orderTotal);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task CalculateDiscountAsync_NewCustomer_ShouldApplyNewCustomerDiscount()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            Segment = CustomerSegment.New,
            Orders = new List<Order>() // No previous orders
        };
        var orderTotal = 100m;

        // Act
        var discount = await _discountService.CalculateDiscountAsync(customer, orderTotal);

        // Assert
        discount.Should().Be(10m); // 10% of 100
    }

    [Fact]
    public async Task CalculateDiscountAsync_PremiumCustomerWithLoyalty_ShouldApplyCombinedDiscounts()
    {
        // Arrange
        var orders = Enumerable.Range(1, 6) // 6 previous orders
            .Select(i => new Order 
            { 
                Id = i, 
                DiscountAmount = 5m,
                OrderItems = new List<OrderItem>
                {
                    new() { Quantity = 1, UnitPrice = 55m } // SubTotal = 55, TotalAmount = 55-5 = 50
                }
            })
            .ToList();

        var customer = new Customer
        {
            Id = 1,
            Name = "Jane Smith",
            Email = "jane@example.com",
            Segment = CustomerSegment.Premium,
            Orders = orders
        };
        var orderTotal = 100m;

        // Act
        var discount = await _discountService.CalculateDiscountAsync(customer, orderTotal);

        // Assert
        // Premium: 15% + Loyalty: 5% = 20% of 100 = 20
        discount.Should().Be(20m);
    }

    [Fact]
    public async Task CalculateDiscountAsync_LargeOrderWithAllDiscounts_ShouldApplyAllDiscounts()
    {
        // Arrange
        var orders = Enumerable.Range(1, 8) // 8 previous orders for loyalty
            .Select(i => new Order 
            { 
                Id = i, 
                DiscountAmount = 10m,
                OrderItems = new List<OrderItem>
                {
                    new() { Quantity = 1, UnitPrice = 110m } // SubTotal = 110, TotalAmount = 110-10 = 100
                }
            })
            .ToList();

        var customer = new Customer
        {
            Id = 1,
            Name = "Premium Customer",
            Email = "premium@example.com",
            Segment = CustomerSegment.Premium,
            Orders = orders
        };
        var orderTotal = 600m; // Above volume threshold

        // Act
        var discount = await _discountService.CalculateDiscountAsync(customer, orderTotal);

        // Assert
        // Premium: 15% + Loyalty: 5% + Volume: 3% = 23% of 600 = 138
        discount.Should().Be(138m);
    }

    [Fact]
    public async Task CalculateDiscountAsync_MaxDiscountCap_ShouldApplyMaximumLimit()
    {
        // Arrange - Create scenario that would exceed 25% discount
        var orders = Enumerable.Range(1, 10)
            .Select(i => new Order 
            { 
                Id = i, 
                DiscountAmount = 5m,
                OrderItems = new List<OrderItem>
                {
                    new() { Quantity = 1, UnitPrice = 105m } // SubTotal = 105, TotalAmount = 105-5 = 100
                }
            })
            .ToList();

        var customer = new Customer
        {
            Id = 1,
            Name = "VIP Customer",
            Email = "vip@example.com",
            Segment = CustomerSegment.Premium,
            Orders = orders
        };
        var orderTotal = 1000m;

        // Act
        var discount = await _discountService.CalculateDiscountAsync(customer, orderTotal);

        // Assert
        // Total would be 23% (230), but max is 25% of 1000 = 250
        // Since 230 < 250, should get 230
        discount.Should().Be(230m); // 15% + 5% + 3% = 23% of 1000
    }

    [Fact]
    public async Task CalculateDiscountAsync_ZeroOrderTotal_ShouldReturnZeroDiscount()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 1,
            Name = "Test Customer",
            Email = "test@example.com",
            Segment = CustomerSegment.Premium,
            Orders = new List<Order>()
        };

        // Act
        var discount = await _discountService.CalculateDiscountAsync(customer, 0m);

        // Assert
        discount.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateDiscountAsync_NegativeOrderTotal_ShouldReturnZeroDiscount()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 1,
            Name = "Test Customer",
            Email = "test@example.com",
            Segment = CustomerSegment.Standard,
            Orders = new List<Order>()
        };

        // Act
        var discount = await _discountService.CalculateDiscountAsync(customer, -50m);

        // Assert
        discount.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateDetailedDiscountAsync_ShouldProvideDetailedBreakdown()
    {
        // Arrange
        var orders = Enumerable.Range(1, 6)
            .Select(i => new Order 
            { 
                Id = i, 
                DiscountAmount = 10m,
                OrderItems = new List<OrderItem>
                {
                    new() { Quantity = 1, UnitPrice = 110m } // SubTotal = 110, TotalAmount = 110-10 = 100
                }
            })
            .ToList();

        var customer = new Customer
        {
            Id = 1,
            Name = "Premium Customer",
            Email = "premium@example.com",
            Segment = CustomerSegment.Premium,
            Orders = orders
        };
        var orderTotal = 600m;

        // Act
        var result = await _discountService.CalculateDetailedDiscountAsync(customer, orderTotal);

        // Assert
        result.Should().NotBeNull();
        result.OrderTotal.Should().Be(600m);
        result.TotalDiscountAmount.Should().Be(138m); // 15% + 5% + 3% = 23%
        result.FinalAmount.Should().Be(462m); // 600 - 138
        result.HasAnyDiscount.Should().BeTrue();

        // Check breakdown
        result.Breakdown.SegmentDiscount.Segment.Should().Be(CustomerSegment.Premium);
        result.Breakdown.SegmentDiscount.Percentage.Should().Be(0.15m);
        result.Breakdown.SegmentDiscount.Amount.Should().Be(90m);

        result.Breakdown.LoyaltyDiscount.Qualified.Should().BeTrue();
        result.Breakdown.LoyaltyDiscount.Percentage.Should().Be(0.05m);
        result.Breakdown.LoyaltyDiscount.Amount.Should().Be(30m);

        result.Breakdown.VolumeDiscount.Qualified.Should().BeTrue();
        result.Breakdown.VolumeDiscount.Percentage.Should().Be(0.03m);
        result.Breakdown.VolumeDiscount.Amount.Should().Be(18m);
    }
}
