using AutoMapper;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDiscountService _discountService;
    private readonly IOrderStatusService _orderStatusService;
    private readonly IAnalyticsService _analyticsService;

    public OrderService(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        IDiscountService discountService,
        IOrderStatusService orderStatusService,
        IAnalyticsService analyticsService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _discountService = discountService;
        _orderStatusService = orderStatusService;
        _analyticsService = analyticsService;
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id);
        return order == null ? null : _mapper.Map<OrderDto>(order);
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByCustomerIdAsync(int customerId)
    {
        var orders = await _unitOfWork.Orders.GetByCustomerIdAsync(customerId);
        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto)
    {

        var customers = await _unitOfWork.Customers.GetAllAsync();
        // Validate customer exists
        var customer = await _unitOfWork.Customers.GetByIdAsync(createOrderDto.CustomerId);
        if (customer == null)
            throw new ArgumentException($"Customer with ID {createOrderDto.CustomerId} not found");

        // Validate products and calculate order total
        var orderItems = new List<OrderItem>();
        decimal subTotal = 0;

        foreach (var item in createOrderDto.OrderItems)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            if (product == null)
                throw new ArgumentException($"Product with ID {item.ProductId} not found");

            if (product.Stock < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for product {product.Name}. Available: {product.Stock}, Requested: {item.Quantity}");

            var orderItem = new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            };

            orderItems.Add(orderItem);
            subTotal += orderItem.LineSubTotal;
        }

        // Calculate discount
        var discountAmount = await _discountService.CalculateDiscountAsync(customer, subTotal);

        // Create order
        var order = new Order
        {
            CustomerId = createOrderDto.CustomerId,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            DiscountAmount = discountAmount,
            Notes = createOrderDto.Notes,
            ShippingAddress = createOrderDto.ShippingAddress,
            OrderItems = orderItems
        };

        // Update product stock
        foreach (var item in orderItems)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                product.Stock -= item.Quantity;
                await _unitOfWork.Products.UpdateAsync(product);
            }
        }

        // Save order
        var createdOrder = await _unitOfWork.Orders.CreateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate analytics cache since we added a new order
        await _analyticsService.InvalidateCacheAsync();

        // Return DTO
        return _mapper.Map<OrderDto>(createdOrder);
    }

    public async Task<OrderStatusTransitionResult> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? notes = null)
    {
        var result = await _orderStatusService.UpdateOrderStatusAsync(orderId, newStatus, notes);
        
        // Invalidate analytics cache when order status changes
        if (result.IsSuccess)
        {
            await _analyticsService.InvalidateCacheAsync();
        }
        
        return result;
    }

    public async Task<List<OrderStatus>> GetValidNextStatusesAsync(int orderId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
            return new List<OrderStatus>();

        return _orderStatusService.GetValidNextStatuses(order.Status);
    }

    public async Task<bool> CanTransitionToStatusAsync(int orderId, OrderStatus newStatus)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null)
            return false;

        return _orderStatusService.IsValidTransition(order.Status, newStatus);
    }

    public async Task<OrderAnalyticsDto> GetOrderAnalyticsAsync()
    {
        return await _analyticsService.GetOrderAnalyticsAsync();
    }
}