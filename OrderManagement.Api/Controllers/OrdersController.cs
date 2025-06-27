using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.DTOs;
using OrderManagement.Application.Services;
using OrderManagement.Application.Models;
using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Interfaces;
using AutoMapper;

namespace OrderManagement.Api.Controllers;

/// <summary>
/// Controller for managing orders
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IDiscountService _discountService;
    private readonly IOrderStatusService _orderStatusService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private readonly IAnalyticsService _analyticsService;
    public OrdersController(
        IOrderService orderService,
        IDiscountService discountService,
        IOrderStatusService orderStatusService,
         IAnalyticsService analyticsService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _orderService = orderService;
        _discountService = discountService;
        _analyticsService = analyticsService;
        _orderStatusService = orderStatusService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets all orders
    /// </summary>
    /// <returns>List of orders</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), 200)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var orders = await _unitOfWork.Orders.GetAllAsync();
        var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);
        return Ok(orderDtos);
    }

    /// <summary>
    /// Gets an order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        return order == null ? NotFound($"Order with ID {id} not found") : Ok(order);
    }

    /// <summary>
    /// Creates a new order with automatic discount calculation
    /// </summary>
    /// <param name="createOrderDto">Order creation data</param>
    /// <returns>Created order</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto createOrderDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var order = await _orderService.CreateOrderAsync(createOrderDto);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to create order: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates order status with validation
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="updateStatusDto">Status update request</param>
    /// <returns>Success or error response</returns>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(OrderStatusTransitionResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<OrderStatusTransitionResult>> UpdateOrderStatus(
        int id,
        [FromBody] UpdateOrderStatusDto updateStatusDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _orderService.UpdateOrderStatusAsync(id, updateStatusDto.NewStatus, updateStatusDto.Notes);

        if (!result.IsSuccess)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets valid next statuses for an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>List of valid next statuses</returns>
    [HttpGet("{id}/valid-statuses")]
    [ProducesResponseType(typeof(List<OrderStatus>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<List<OrderStatus>>> GetValidNextStatuses(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound($"Order with ID {id} not found");

        var validStatuses = await _orderService.GetValidNextStatusesAsync(id);
        return Ok(validStatuses);
    }

    /// <summary>
    /// Checks if a status transition is valid
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="status">Status to check</param>
    /// <returns>True if transition is valid</returns>
    [HttpGet("{id}/can-transition-to/{status}")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<bool>> CanTransitionToStatus(int id, OrderStatus status)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound($"Order with ID {id} not found");

        var canTransition = await _orderService.CanTransitionToStatusAsync(id, status);
        return Ok(canTransition);
    }

    /// <summary>
    /// Calculates discount for a potential order without creating it
    /// </summary>
    /// <param name="calculateDiscountDto">Discount calculation request</param>
    /// <returns>Calculated discount amount</returns>
    [HttpPost("calculate-discount")]
    [ProducesResponseType(typeof(DiscountCalculationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<DiscountCalculationResult>> CalculateDiscount([FromBody] CalculateDiscountDto calculateDiscountDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(calculateDiscountDto.CustomerId);
            if (customer == null)
                return NotFound($"Customer with ID {calculateDiscountDto.CustomerId} not found");

            var discountResult = await _discountService.CalculateDetailedDiscountAsync(customer, calculateDiscountDto.OrderTotal);
            return Ok(discountResult);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to calculate discount: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets orders by status
    /// </summary>
    /// <param name="status">Order status</param>
    /// <returns>Orders with the specified status</returns>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), 200)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByStatus(OrderStatus status)
    {
        var orders = await _unitOfWork.Orders.GetAllAsync();
        var filteredOrders = orders.Where(o => o.Status == status);
        var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(filteredOrders);
        return Ok(orderDtos);
    }

    /// <summary>
    /// Gets orders by customer ID
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <returns>Customer's orders</returns>
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByCustomer(int customerId)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
        if (customer == null)
            return NotFound($"Customer with ID {customerId} not found");

        var orders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
        return Ok(orders);
    }

    /// <summary>
    /// Gets comprehensive order analytics with caching
    /// </summary>
    /// <param name="forceRefresh">If true, bypasses cache and recalculates</param>
    /// <returns>Detailed analytics data</returns>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(OrderAnalyticsDto), 200)]
    public async Task<ActionResult<OrderAnalyticsDto>> GetAnalytics([FromQuery] bool forceRefresh = false)
    {
        var analytics = await _analyticsService.GetOrderAnalyticsAsync(forceRefresh);
        return Ok(analytics);
    }

    /// <summary>
    /// Gets analytics for a specific date range
    /// </summary>
    /// <param name="startDate">Start date (YYYY-MM-DD)</param>
    /// <param name="endDate">End date (YYYY-MM-DD)</param>
    /// <returns>Analytics for the specified period</returns>
    [HttpGet("analytics/period")]
    [ProducesResponseType(typeof(OrderAnalyticsDto), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<OrderAnalyticsDto>> GetAnalyticsForPeriod(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest("Start date cannot be after end date");

        if (endDate > DateTime.UtcNow.Date.AddDays(1))
            return BadRequest("End date cannot be in the future");

        var analytics = await _analyticsService.GetAnalyticsForPeriodAsync(startDate, endDate);
        return Ok(analytics);
    }

    /// <summary>
    /// Invalidates the analytics cache manually
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("analytics/refresh")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> RefreshAnalyticsCache()
    {
        await _analyticsService.InvalidateCacheAsync();
        return Ok(new { message = "Analytics cache has been invalidated" });
    }


    /// <summary>
    /// Cancels an order
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="reason">Cancellation reason</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(OrderStatusTransitionResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<OrderStatusTransitionResult>> CancelOrder(int id, [FromBody] string reason = "")
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Cancelled, $"Cancelled: {reason}");

        if (!result.IsSuccess)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }
}