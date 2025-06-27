

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(OrderManagementDbContext context, ILogger logger)
    {
        try
        {
            // Check if data already exists
            if (await context.Products.AnyAsync())
            {
                logger.LogInformation("Database already seeded");
                return;
            }

            // Seed Products
            var products = new List<Product>
            {
                new() { Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, Stock = 50, Category = "Electronics", SKU = "LAP001" },
                new() { Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, Stock = 100, Category = "Electronics", SKU = "MOU001" },
                new() { Name = "Keyboard", Description = "Mechanical keyboard", Price = 79.99m, Stock = 75, Category = "Electronics", SKU = "KEY001" },
                new() { Name = "Monitor", Description = "24-inch monitor", Price = 299.99m, Stock = 30, Category = "Electronics", SKU = "MON001" },
                new() { Name = "Headphones", Description = "Noise-cancelling headphones", Price = 149.99m, Stock = 40, Category = "Electronics", SKU = "HEA001" },
                new() { Name = "Webcam", Description = "4K webcam", Price = 89.99m, Stock = 25, Category = "Electronics", SKU = "WEB001" }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            // Seed Customers
            var customers = new List<Customer>
            {
                new() { Name = "John Doe", Email = "john@example.com", Segment = CustomerSegment.New },
                new() { Name = "Jane Smith", Email = "jane@example.com", Segment = CustomerSegment.Premium },
                new() { Name = "Bob Johnson", Email = "bob@example.com", Segment = CustomerSegment.Standard },
                new() { Name = "Alice Brown", Email = "alice@example.com", Segment = CustomerSegment.Premium },
                new() { Name = "Charlie Davis", Email = "charlie@example.com", Segment = CustomerSegment.Standard }
            };

            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();

            // Seed some sample orders
            var john = customers.First(c => c.Name == "John Doe");
            var jane = customers.First(c => c.Name == "Jane Smith");
            var laptop = products.First(p => p.Name == "Laptop");
            var mouse = products.First(p => p.Name == "Mouse");

            var sampleOrders = new List<Order>
            {
                new()
                {
                    CustomerId = jane.Id,
                    Status = OrderStatus.Delivered,
                    OrderDate = DateTime.UtcNow.AddDays(-30),
                    ShippedDate = DateTime.UtcNow.AddDays(-28),
                    DeliveredDate = DateTime.UtcNow.AddDays(-25),
                    DiscountAmount = 150.00m,
                    ShippingAddress = "123 Main St, Anytown, USA",
                    OrderItems = new List<OrderItem>
                    {
                        new() { ProductId = laptop.Id, Quantity = 1, UnitPrice = laptop.Price }
                    }
                },
                new()
                {
                    CustomerId = john.Id,
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.UtcNow.AddDays(-2),
                    DiscountAmount = 3.00m,
                    ShippingAddress = "456 Oak Ave, Another City, USA",
                    OrderItems = new List<OrderItem>
                    {
                        new() { ProductId = mouse.Id, Quantity = 1, UnitPrice = mouse.Price }
                    }
                }
            };

            await context.Orders.AddRangeAsync(sampleOrders);
            await context.SaveChangesAsync();

            logger.LogInformation("Database seeded successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
