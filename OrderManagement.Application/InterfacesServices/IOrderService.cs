using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Services;

public interface IOrderService
{
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<IEnumerable<OrderDto>> GetOrdersByCustomerIdAsync(int customerId);
    Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto);
   
    // Status management methods
    Task<OrderStatusTransitionResult> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? notes = null);
    Task<List<OrderStatus>> GetValidNextStatusesAsync(int orderId);
    Task<bool> CanTransitionToStatusAsync(int orderId, OrderStatus newStatus);
}