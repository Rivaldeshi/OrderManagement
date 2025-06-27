using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderManagementDbContext _context;

    public OrderRepository(OrderManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order> CreateAsync(Order order)
    {
        order.CreatedAt = DateTime.UtcNow;
        
        // Set CreatedAt for order items
        foreach (var item in order.OrderItems)
        {
            item.CreatedAt = DateTime.UtcNow;
        }
        
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        // Reload with navigation properties
        return await GetByIdAsync(order.Id) ?? order;
    }

    public async Task<Order?> UpdateAsync(Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _context.Entry(order).State = EntityState.Modified;
        
        try
        {
            await _context.SaveChangesAsync();
            return await GetByIdAsync(order.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await OrderExistsAsync(order.Id))
                return null;
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return false;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> OrderExistsAsync(int id)
    {
        return await _context.Orders.AnyAsync(e => e.Id == id);
    }
}
