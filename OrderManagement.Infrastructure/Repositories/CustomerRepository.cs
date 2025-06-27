using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly OrderManagementDbContext _context;

    public CustomerRepository(OrderManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers
            .Include(c => c.Orders)
                .ThenInclude(o => o.OrderItems)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await _context.Customers
            .Include(c => c.Orders)
            .ToListAsync();
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        customer.CreatedAt = DateTime.UtcNow;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer?> UpdateAsync(Customer customer)
    {
        customer.UpdatedAt = DateTime.UtcNow;
        _context.Entry(customer).State = EntityState.Modified;
        
        try
        {
            await _context.SaveChangesAsync();
            return customer;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await CustomerExistsAsync(customer.Id))
                return null;
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return false;

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> CustomerExistsAsync(int id)
    {
        return await _context.Customers.AnyAsync(e => e.Id == id);
    }
}