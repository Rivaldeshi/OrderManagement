using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.DTOs;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Entities;
using AutoMapper;

namespace OrderManagement.Api.Controllers;

/// <summary>
/// Controller for managing customers
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CustomersController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets all customers
    /// </summary>
    /// <returns>List of customers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), 200)]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
    {
        var customers = await _unitOfWork.Customers.GetAllAsync();
        var customerDtos = _mapper.Map<IEnumerable<CustomerDto>>(customers);
        return Ok(customerDtos);
    }

    /// <summary>
    /// Gets a customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Customer details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        
        if (customer == null)
            return NotFound($"Customer with ID {id} not found");

        var customerDto = _mapper.Map<CustomerDto>(customer);
        return Ok(customerDto);
    }

    /// <summary>
    /// Creates a new customer
    /// </summary>
    /// <param name="createCustomerDto">Customer creation data</param>
    /// <returns>Created customer</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerDto createCustomerDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var customer = _mapper.Map<Customer>(createCustomerDto);
            var createdCustomer = await _unitOfWork.Customers.CreateAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            var customerDto = _mapper.Map<CustomerDto>(createdCustomer);
            return CreatedAtAction(nameof(GetCustomer), new { id = customerDto.Id }, customerDto);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to create customer: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="updateCustomerDto">Customer update data</param>
    /// <returns>Updated customer</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(int id, UpdateCustomerDto updateCustomerDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingCustomer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (existingCustomer == null)
            return NotFound($"Customer with ID {id} not found");

        try
        {
            // Update properties
            existingCustomer.Name = updateCustomerDto.Name;
            existingCustomer.Email = updateCustomerDto.Email;
            existingCustomer.Segment = updateCustomerDto.Segment;

            var updatedCustomer = await _unitOfWork.Customers.UpdateAsync(existingCustomer);
            await _unitOfWork.SaveChangesAsync();

            var customerDto = _mapper.Map<CustomerDto>(updatedCustomer);
            return Ok(customerDto);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to update customer: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer == null)
            return NotFound($"Customer with ID {id} not found");

        try
        {
            await _unitOfWork.Customers.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to delete customer: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets orders for a specific customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Customer's orders</returns>
    [HttpGet("{id}/orders")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetCustomerOrders(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer == null)
            return NotFound($"Customer with ID {id} not found");

        var orders = await _unitOfWork.Orders.GetByCustomerIdAsync(id);
        var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);
        return Ok(orderDtos);
    }
}