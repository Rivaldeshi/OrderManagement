using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Models;

/// <summary>
/// Represents a status transition request
/// </summary>
public class UpdateOrderStatusRequest
{
    public OrderStatus NewStatus { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Result of a status transition attempt
/// </summary>
public class OrderStatusTransitionResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public OrderStatus? PreviousStatus { get; set; }
    public OrderStatus? NewStatus { get; set; }
    public DateTime? TransitionDate { get; set; }
    public List<string> Errors { get; set; } = new();
    
    public static OrderStatusTransitionResult Success(OrderStatus previousStatus, OrderStatus newStatus, string message = "")
    {
        return new OrderStatusTransitionResult
        {
            IsSuccess = true,
            Message = string.IsNullOrEmpty(message) ? $"Order status updated from {previousStatus} to {newStatus}" : message,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            TransitionDate = DateTime.UtcNow
        };
    }
    
    public static OrderStatusTransitionResult Failure(string error)
    {
        return new OrderStatusTransitionResult
        {
            IsSuccess = false,
            Message = error,
            Errors = { error }
        };
    }
    
    public static OrderStatusTransitionResult Failure(List<string> errors)
    {
        return new OrderStatusTransitionResult
        {
            IsSuccess = false,
            Message = "Status transition failed",
            Errors = errors
        };
    }
}