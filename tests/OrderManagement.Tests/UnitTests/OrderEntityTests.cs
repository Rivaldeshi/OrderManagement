using FluentAssertions;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using Xunit;

namespace OrderManagement.Tests.UnitTests;

public class OrderEntityTests
{
    [Fact]
    public void Order_ComputedProperties_ShouldCalculateCorrectly()
    {
        // Arrange
        var order = new Order
        {
            Id = 1,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow.AddDays(-2),
            DiscountAmount = 50m,
            OrderItems = new List<OrderItem>
            {
                new() { ProductId = 1, Quantity = 2, UnitPrice = 100m },
                new() { ProductId = 2, Quantity = 1, UnitPrice = 150m }
            }
        };

        // Act & Assert
        order.SubTotal.Should().Be(350m); // (2 * 100) + (1 * 150)
        order.TotalAmount.Should().Be(300m); // 350 - 50
        order.TotalItemsCount.Should().Be(3); // 2 + 1
        order.UniqueProductsCount.Should().Be(2); // 2 different products
        order.HasDiscount.Should().BeTrue();
        order.DiscountPercentage.Should().BeApproximately(14.29m, 0.01m); // 50/350 * 100
        order.CanBeCancelled.Should().BeTrue();
        order.CanBeShipped.Should().BeTrue();
        order.CanBeDelivered.Should().BeFalse();
    }

    [Fact]
    public void Order_FulfillmentTimes_ShouldCalculateCorrectly()
    {
        // Arrange - Use specific dates to avoid calculation errors
        var orderDate = new DateTime(2024, 1, 1, 10, 0, 0);
        var shippedDate = new DateTime(2024, 1, 2, 14, 0, 0);  // 1 day + 4 hours later
        var deliveredDate = new DateTime(2024, 1, 5, 16, 0, 0); // 3 days + 2 hours after shipped

        var order = new Order
        {
            OrderDate = orderDate,
            ShippedDate = shippedDate,
            DeliveredDate = deliveredDate,
            Status = OrderStatus.Delivered
        };

        // Act & Assert
        // Processing time: from order to shipped = 1 day + 4 hours = 28 hours
        order.ProcessingTime.Should().Be(TimeSpan.FromHours(28));
        
        // Delivery time: from shipped to delivered = 3 days + 2 hours = 74 hours
        order.DeliveryTime.Should().Be(TimeSpan.FromHours(74));
        
        // Total fulfillment time: from order to delivered = 4 days + 6 hours = 102 hours
        order.TotalFulfillmentTime.Should().Be(TimeSpan.FromHours(102));
    }

    [Fact]
    public void OrderItem_ComputedProperties_ShouldCalculateCorrectly()
    {
        // Arrange
        var orderItem = new OrderItem
        {
            Quantity = 3,
            UnitPrice = 50m,
            DiscountAmount = 15m
        };

        // Act & Assert
        orderItem.LineSubTotal.Should().Be(150m); // 3 * 50
        orderItem.LineTotal.Should().Be(135m); // 150 - 15
        orderItem.HasDiscount.Should().BeTrue();
        orderItem.DiscountPercentage.Should().Be(10m); // 15/150 * 100
        orderItem.EffectiveUnitPrice.Should().Be(45m); // 135/3
        orderItem.TotalSavings.Should().Be(15m);
    }

    [Fact]
    public void Customer_ComputedProperties_ShouldCalculateCorrectly()
    {
        // Arrange
        var customer = new Customer
        {
            Orders = new List<Order>
            {
                new() 
                { 
                    OrderDate = DateTime.UtcNow.AddDays(-10),
                    DiscountAmount = 10m,
                    OrderItems = new List<OrderItem>
                    {
                        new() { Quantity = 1, UnitPrice = 100m } // SubTotal = 100, TotalAmount = 90
                    }
                },
                new() 
                { 
                    OrderDate = DateTime.UtcNow.AddDays(-5),
                    DiscountAmount = 0m,
                    OrderItems = new List<OrderItem>
                    {
                        new() { Quantity = 2, UnitPrice = 75m } // SubTotal = 150, TotalAmount = 150
                    }
                }
            }
        };

        // Act & Assert
        customer.TotalOrdersCount.Should().Be(2);
        customer.TotalSpent.Should().Be(240m); // 90 + 150
        customer.LastOrderDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(-5), TimeSpan.FromMinutes(1));
        customer.IsNewCustomer.Should().BeFalse();
        customer.IsFrequentCustomer.Should().BeFalse(); // Only 2 orders, needs > 5
    }

    [Fact]
    public void Product_StockProperties_ShouldCalculateCorrectly()
    {
        // Test in stock
        var inStockProduct = new Product { Stock = 25 };
        inStockProduct.IsInStock.Should().BeTrue();
        inStockProduct.IsLowStock.Should().BeFalse();
        inStockProduct.IsOutOfStock.Should().BeFalse();
        inStockProduct.StockStatus.Should().Be("In Stock");

        // Test low stock
        var lowStockProduct = new Product { Stock = 5 };
        lowStockProduct.IsInStock.Should().BeTrue();
        lowStockProduct.IsLowStock.Should().BeTrue();
        lowStockProduct.IsOutOfStock.Should().BeFalse();
        lowStockProduct.StockStatus.Should().Be("Low Stock");

        // Test out of stock
        var outOfStockProduct = new Product { Stock = 0 };
        outOfStockProduct.IsInStock.Should().BeFalse();
        outOfStockProduct.IsLowStock.Should().BeFalse();
        outOfStockProduct.IsOutOfStock.Should().BeTrue();
        outOfStockProduct.StockStatus.Should().Be("Out of Stock");
    }
}