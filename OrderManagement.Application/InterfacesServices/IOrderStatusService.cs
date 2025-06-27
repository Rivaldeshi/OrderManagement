using OrderManagement.Application.Models;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Services;

/// <summary>
/// Service for managing order status transitions
/// </summary>
public interface IOrderStatusService
{
    /// <summary>
    /// Validates if a status transition is allowed
    /// </summary>
    /// <param name="currentStatus">Current order status</param>
    /// <param name="newStatus">Desired new status</param>
    /// <returns>True if transition is valid</returns>
    bool IsValidTransition(OrderStatus currentStatus, OrderStatus newStatus);

    /// <summary>
    /// Gets all valid next statuses for the current status
    /// </summary>
    /// <param name="currentStatus">Current order status</param>
    /// <returns>List of valid next statuses</returns>
    List<OrderStatus> GetValidNextStatuses(OrderStatus currentStatus);

    /// <summary>
    /// Gets a human-readable description of why a transition is invalid
    /// </summary>
    /// <param name="currentStatus">Current order status</param>
    /// <param name="newStatus">Desired new status</param>
    /// <returns>Error message explaining why transition is invalid</returns>
    string GetTransitionErrorMessage(OrderStatus currentStatus, OrderStatus newStatus);

    /// <summary>
    /// Updates an order's status with validation
    /// </summary>
    /// <param name="orderId">Order ID to update</param>
    /// <param name="newStatus">New status to set</param>
    /// <param name="notes">Optional notes for the transition</param>
    /// <returns>Result of the transition attempt</returns>
    Task<OrderStatusTransitionResult> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? notes = null);
}