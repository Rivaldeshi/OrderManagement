using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OrderManagement.Application.Services;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Interfaces;
using Xunit;

namespace OrderManagement.Tests.UnitTests;

public class OrderStatusServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly OrderStatusService _orderStatusService;

    public OrderStatusServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockOrderRepository.Object);
        _orderStatusService = new OrderStatusService(_mockUnitOfWork.Object);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Pending, OrderStatus.Pending, false)] // Same status
    public void IsValidTransition_ShouldReturnCorrectResult(OrderStatus current, OrderStatus newStatus, bool expected)
    {
        // Act
        var result = _orderStatusService.IsValidTransition(current, newStatus);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetValidNextStatuses_PendingOrder_ShouldReturnOnlyShipped()
    {
        // Act
        var result = _orderStatusService.GetValidNextStatuses(OrderStatus.Pending);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void GetValidNextStatuses_ShippedOrder_ShouldReturnDeliveredAndCancelled()
    {
        // Act
        var result = _orderStatusService.GetValidNextStatuses(OrderStatus.Shipped);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(OrderStatus.Delivered);
        result.Should().Contain(OrderStatus.Cancelled);
    }

    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void GetValidNextStatuses_FinalStatuses_ShouldReturnEmpty(OrderStatus finalStatus)
    {
        // Act
        var result = _orderStatusService.GetValidNextStatuses(finalStatus);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ValidTransition_ShouldSucceed()
    {
        // Arrange
        var order = new Order
        {
            Id = 1,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow.AddDays(-1)
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(order);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(order);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _orderStatusService.UpdateOrderStatusAsync(1, OrderStatus.Shipped, "Order shipped");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.PreviousStatus.Should().Be(OrderStatus.Pending);
        result.NewStatus.Should().Be(OrderStatus.Shipped);
        
        // Verify order was updated
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedDate.Should().NotBeNull();
        order.UpdatedAt.Should().NotBeNull();
        order.Notes.Should().Contain("Order shipped");
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_InvalidTransition_ShouldFail()
    {
        // Arrange
        var order = new Order
        {
            Id = 1,
            Status = OrderStatus.Delivered // Final status
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(order);

        // Act
        var result = await _orderStatusService.UpdateOrderStatusAsync(1, OrderStatus.Pending, "Try to revert");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("cannot be changed");
        
        // Verify repository methods were not called
        _mockOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_OrderNotFound_ShouldFail()
    {
        // Arrange
        _mockOrderRepository.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _orderStatusService.UpdateOrderStatusAsync(999, OrderStatus.Shipped);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public void GetTransitionErrorMessage_ShouldProvideHelpfulErrorMessages()
    {
        // Test same status
        var result1 = _orderStatusService.GetTransitionErrorMessage(OrderStatus.Pending, OrderStatus.Pending);
        result1.Should().Contain("already in Pending status");

        // Test invalid transition
        var result2 = _orderStatusService.GetTransitionErrorMessage(OrderStatus.Pending, OrderStatus.Delivered);
        result2.Should().Contain("Cannot change status from Pending to Delivered");
        result2.Should().Contain("Valid transitions are: Shipped");

        // Test final status
        var result3 = _orderStatusService.GetTransitionErrorMessage(OrderStatus.Delivered, OrderStatus.Pending);
        result3.Should().Contain("cannot be changed to any other status");
    }
}
