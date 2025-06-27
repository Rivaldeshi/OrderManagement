
using System.Composition;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Data;
using Xunit;
using Xunit.Abstractions;

namespace OrderManagement.Tests.IntegrationTests;

public class OrdersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public OrdersControllerIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _output = output;

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                
                services.AddDbContext<OrderManagementDbContext>(options =>
                {
                    options.UseInMemoryDatabase("SharedTestDatabase"); // NOM FIXE
                    options.EnableSensitiveDataLogging();
                });
            });
        });

        _client = _factory.CreateClient();

        // Initialiser la base de données
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();

            // Créer la base de données
            context.Database.EnsureCreated();

        
            context.Orders.RemoveRange(context.Orders);
            context.OrderItems.RemoveRange(context.OrderItems);
            context.Customers.RemoveRange(context.Customers);
            context.Products.RemoveRange(context.Products);
            context.SaveChanges();

            var customers = new[]
            {
                new Customer
                {
                    Id = 1, // ID explicite
                    Name = "John Doe",
                    Email = "john@test.com",
                    Segment = CustomerSegment.New,
                    CreatedAt = DateTime.UtcNow
                },
                new Customer
                {
                    Id = 2, // ID explicite
                    Name = "Jane Smith",
                    Email = "jane@test.com",
                    Segment = CustomerSegment.Premium,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var products = new[]
            {
                new Product
                {
                    Id = 1, // ID explicite
                    Name = "Test Laptop",
                    Price = 999.99m,
                    Stock = 10,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Product
                {
                    Id = 2, // ID explicite
                    Name = "Test Mouse",
                    Price = 29.99m,
                    Stock = 50,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            };

            context.Customers.AddRange(customers);
            context.Products.AddRange(products);
            context.SaveChanges();

           
            var customerCount = context.Customers.Count();
            var productCount = context.Products.Count();
            var customer1 = context.Customers.Find(1);
            var customer2 = context.Customers.Find(2);

            _output.WriteLine("Database initialized successfully");
            _output.WriteLine($"Customers: {customerCount}");
            _output.WriteLine($"Products: {productCount}");
            _output.WriteLine($"Customer 1: {customer1?.Name ?? "NOT FOUND"}");
            _output.WriteLine($"Customer 2: {customer2?.Name ?? "NOT FOUND"}");

           
            TestDatabaseVisibility();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Database initialization failed: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }


    private void TestDatabaseVisibility()
    {
        try
        {
            using var apiScope = _factory.Services.CreateScope();
            var apiContext = apiScope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();

            var apiCustomerCount = apiContext.Customers.Count();
            var apiProductCount = apiContext.Products.Count();
            var apiCustomer1 = apiContext.Customers.Find(1);

            _output.WriteLine("=== API Context Visibility Test ===");
            _output.WriteLine($"API sees Customers: {apiCustomerCount}");
            _output.WriteLine($"API sees Products: {apiProductCount}");
            _output.WriteLine($"API sees Customer 1: {apiCustomer1?.Name ?? "NOT FOUND"}");

            if (apiCustomerCount == 0)
            {
                _output.WriteLine("❌ PROBLEM: API context doesn't see the seeded data!");
            }
            else
            {
                _output.WriteLine("✅ SUCCESS: API context sees the seeded data!");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"API visibility test failed: {ex.Message}");
        }
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ShouldReturnCreatedOrder()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = 1,
            ShippingAddress = "123 Test Street",
            Notes = "Test order",
            OrderItems = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 1 },
                new() { ProductId = 2, Quantity = 2 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createOrderDto);

        // Debug output
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Status: {response.StatusCode}");
        _output.WriteLine($"Response Content: {responseContent}");

        // Assert
        if (response.StatusCode != HttpStatusCode.Created)
        {
            _output.WriteLine($"Expected 201 Created, got {response.StatusCode}");
            _output.WriteLine($"Response body: {responseContent}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdOrder = await response.Content.ReadFromJsonAsync<OrderDto>();
        createdOrder.Should().NotBeNull();
        createdOrder!.CustomerId.Should().Be(1);
        createdOrder.Status.Should().Be(OrderStatus.Pending);
        createdOrder.OrderItems.Should().HaveCount(2);
        createdOrder.TotalAmount.Should().BeGreaterThan(0);
        createdOrder.HasDiscount.Should().BeTrue(); // New customer should get discount
    }

    [Fact]
    public async Task CreateOrder_NonExistentCustomer_ShouldReturnBadRequest()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = 999, // Non-existent customer
            OrderItems = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createOrderDto);

        // Debug output
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Status: {response.StatusCode}");
        _output.WriteLine($"Response Content: {responseContent}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_NonExistentProduct_ShouldReturnBadRequest()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = 1,
            OrderItems = new List<CreateOrderItemDto>
            {
                new() { ProductId = 999, Quantity = 1 } // Non-existent product
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createOrderDto);

        // Debug output
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Status: {response.StatusCode}");
        _output.WriteLine($"Response Content: {responseContent}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_InsufficientStock_ShouldReturnBadRequest()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = 1,
            OrderItems = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 100 } // More than available stock (10)
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createOrderDto);

        // Debug output
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Status: {response.StatusCode}");
        _output.WriteLine($"Response Content: {responseContent}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrder_ExistingOrder_ShouldReturnOrder()
    {
        // Arrange - First create an order
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = 2,
            OrderItems = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createOrderDto);

        // Debug the creation
        var createContent = await createResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Create Response Status: {createResponse.StatusCode}");
        _output.WriteLine($"Create Response Content: {createContent}");

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        createdOrder.Should().NotBeNull();

        // Act
        var response = await _client.GetAsync($"/api/orders/{createdOrder!.Id}");

        // Debug output
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Get Response Status: {response.StatusCode}");
        _output.WriteLine($"Get Response Content: {responseContent}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Id.Should().Be(createdOrder.Id);
        order.Customer.Should().NotBeNull();
        order.OrderItems.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetOrder_NonExistentOrder_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/orders/999");

        // Debug output
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Status: {response.StatusCode}");
        _output.WriteLine($"Response Content: {responseContent}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateOrderStatus_ValidTransition_ShouldReturnSuccess()
    {
        // Arrange - Create an order first
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = 1,
            OrderItems = new List<CreateOrderItemDto>
            {
                new() { ProductId = 2, Quantity = 1 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createOrderDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        createdOrder.Should().NotBeNull();

        var updateStatusDto = new UpdateOrderStatusDto
        {
            NewStatus = OrderStatus.Shipped,
            Notes = "Order has been shipped"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/orders/{createdOrder!.Id}/status", updateStatusDto);

        // Debug output
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Update Status Response: {response.StatusCode}");
        _output.WriteLine($"Update Status Content: {responseContent}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderStatusTransitionResult>();
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.PreviousStatus.Should().Be(OrderStatus.Pending);
        result.NewStatus.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public async Task UpdateOrderStatus_InvalidTransition_ShouldReturnBadRequest()
    {
        // Arrange - Create an order and manually set it to delivered
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();

        var order = new Order
        {
            Id = 100, // ID explicite pour éviter les conflits
            CustomerId = 1,
            Status = OrderStatus.Delivered, // Already delivered
            OrderDate = DateTime.UtcNow.AddDays(-5),
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem>() // Ajouter une liste vide pour éviter les problèmes
        };

        context.Orders.Add(order);
        context.SaveChanges();

        // Vérifier que l'ordre a bien été créé
        var savedOrder = context.Orders.Find(100);
        _output.WriteLine($"Created order with ID: {savedOrder?.Id}, Status: {savedOrder?.Status}");

        var updateStatusDto = new UpdateOrderStatusDto
        {
            NewStatus = OrderStatus.Pending, // Invalid: can't go back to pending
            Notes = "Try to revert"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/orders/100/status", updateStatusDto);

        // Debug output
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Invalid Transition Response: {response.StatusCode}");
        _output.WriteLine($"Invalid Transition Content: {responseContent}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAnalytics_ShouldReturnAnalyticsData()
    {
        // Arrange - Create some orders for analytics
        await CreateTestOrdersForAnalytics();

        // Act
        var response = await _client.GetAsync("/api/orders/analytics");

        // Debug output
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Analytics Response Status: {response.StatusCode}");
        _output.WriteLine($"Analytics Response Content: {responseContent}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var analytics = await response.Content.ReadFromJsonAsync<OrderAnalyticsDto>();
        analytics.Should().NotBeNull();
        analytics!.TotalOrders.Should().BeGreaterThanOrEqualTo(0);
        analytics.TotalCustomers.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetValidNextStatuses_ShouldReturnValidStatuses()
    {
        // Arrange - Create a pending order
        var createOrderDto = new CreateOrderDto
        {
            CustomerId = 1,
            OrderItems = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createOrderDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        createdOrder.Should().NotBeNull();

        // Act
        var response = await _client.GetAsync($"/api/orders/{createdOrder!.Id}/valid-statuses");

        // Debug output
        var responseContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Valid Statuses Response: {response.StatusCode}");
        _output.WriteLine($"Valid Statuses Content: {responseContent}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var validStatuses = await response.Content.ReadFromJsonAsync<List<OrderStatus>>();
        validStatuses.Should().NotBeNull();
        validStatuses.Should().ContainSingle()
            .Which.Should().Be(OrderStatus.Shipped);
    }

    private async Task CreateTestOrdersForAnalytics()
    {
        var orders = new[]
        {
            new CreateOrderDto
            {
                CustomerId = 1,
                OrderItems = new List<CreateOrderItemDto>
                {
                    new() { ProductId = 1, Quantity = 1 }
                }
            },
            new CreateOrderDto
            {
                CustomerId = 2,
                OrderItems = new List<CreateOrderItemDto>
                {
                    new() { ProductId = 2, Quantity = 3 }
                }
            }
        };

        foreach (var order in orders)
        {
            var response = await _client.PostAsJsonAsync("/api/orders", order);

            // Debug
            if (response.StatusCode != HttpStatusCode.Created)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"Failed to create test order: {response.StatusCode} - {errorContent}");
            }

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
