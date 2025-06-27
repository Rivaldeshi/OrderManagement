using System.Diagnostics;
using FluentAssertions;
using OrderManagement.Application.Services;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace OrderManagement.Tests.PerformanceTests;

public class DiscountCalculationPerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly DiscountService _discountService;

    public DiscountCalculationPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _discountService = new DiscountService();
    }

    [Fact]
    public async Task CalculateDiscountAsync_BulkCalculations_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        const int iterations = 1000;
        var customers = CreateTestCustomers(iterations);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = customers.Select(customer => 
            _discountService.CalculateDiscountAsync(customer, 500m));
        
        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(iterations);
        results.Should().OnlyContain(r => r >= 0);
        
        _output.WriteLine($"Calculated {iterations} discounts in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per calculation: {(double)stopwatch.ElapsedMilliseconds / iterations:F2}ms");
        
        // Performance assertion - should complete within reasonable time
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds max
    }

    private static List<Customer> CreateTestCustomers(int count)
    {
        var customers = new List<Customer>();
        var segments = Enum.GetValues<CustomerSegment>();
        
        for (int i = 0; i < count; i++)
        {
            var orders = Enumerable.Range(0, i % 10) // Varying order count
                .Select(j => new Order 
                { 
                    DiscountAmount = j * 5m,
                    OrderItems = new List<OrderItem>
                    {
                        new() { Quantity = 1, UnitPrice = 50m + (j * 10) } // Varying prices
                    }
                })
                .ToList();

            customers.Add(new Customer
            {
                Id = i + 1,
                Name = $"Customer {i + 1}",
                Email = $"customer{i + 1}@test.com",
                Segment = segments[i % segments.Length],
                Orders = orders
            });
        }
        
        return customers;
    }
}
