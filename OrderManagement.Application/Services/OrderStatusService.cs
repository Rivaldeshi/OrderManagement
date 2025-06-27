using OrderManagement.Application.Models;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Interfaces;

namespace OrderManagement.Application.Services;

public class OrderStatusService : IOrderStatusService
{
    private readonly IUnitOfWork _unitOfWork;

    // Define valid status transitions
    private readonly Dictionary<OrderStatus, List<OrderStatus>> _validTransitions = new()
    {
        { OrderStatus.Pending, new List<OrderStatus> { OrderStatus.Shipped } },
        { OrderStatus.Shipped, new List<OrderStatus> { OrderStatus.Delivered, OrderStatus.Cancelled } },
        { OrderStatus.Delivered, new List<OrderStatus>() }, // Final status - no transitions
        { OrderStatus.Cancelled, new List<OrderStatus>() }  // Final status - no transitions
    };

    public OrderStatusService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public bool IsValidTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        if (currentStatus == newStatus)
            return false; // Same status is not a valid transition

        return _validTransitions.ContainsKey(currentStatus) &&
               _validTransitions[currentStatus].Contains(newStatus);
    }

    public List<OrderStatus> GetValidNextStatuses(OrderStatus currentStatus)
    {
        return _validTransitions.ContainsKey(currentStatus)
            ? _validTransitions[currentStatus]
            : new List<OrderStatus>();
    }

    public string GetTransitionErrorMessage(OrderStatus currentStatus, OrderStatus newStatus)
    {
        if (currentStatus == newStatus)
            return $"Order is already in {currentStatus} status";

        if (!_validTransitions.ContainsKey(currentStatus))
            return $"Invalid current status: {currentStatus}";

        var validNextStatuses = _validTransitions[currentStatus];

        if (validNextStatuses.Count == 0)
            return $"Order with status {currentStatus} cannot be changed to any other status";

        if (!validNextStatuses.Contains(newStatus))
        {
            var validStatusesText = string.Join(", ", validNextStatuses);
            return $"Cannot change status from {currentStatus} to {newStatus}. Valid transitions are: {validStatusesText}";
        }

        return "Unknown error";
    }

    public async Task<OrderStatusTransitionResult> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? notes = null)
    {
        // Get the order
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
            return OrderStatusTransitionResult.Failure($"Order with ID {orderId} not found");

        var currentStatus = order.Status;

        // Validate the transition
        if (!IsValidTransition(currentStatus, newStatus))
        {
            var errorMessage = GetTransitionErrorMessage(currentStatus, newStatus);
            return OrderStatusTransitionResult.Failure(errorMessage);
        }

        // Update the order status
        var previousStatus = order.Status;
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        // Set specific timestamps based on status
        switch (newStatus)
        {
            case OrderStatus.Shipped:
                order.ShippedDate = DateTime.UtcNow;
                break;
            case OrderStatus.Delivered:
                order.DeliveredDate = DateTime.UtcNow;
                break;
        }

        // Add notes if provided
        if (!string.IsNullOrWhiteSpace(notes))
        {
            order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                ? $"Status Update: {notes}"
                : $"{order.Notes}\nStatus Update: {notes}";
        }

        // Save changes
        try
        {
            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            return OrderStatusTransitionResult.Success(
                previousStatus,
                newStatus,
                $"Order {orderId} status successfully updated from {previousStatus} to {newStatus}"
            );
        }
        catch (Exception ex)
        {
            return OrderStatusTransitionResult.Failure($"Failed to update order status: {ex.Message}");
        }
    }
}