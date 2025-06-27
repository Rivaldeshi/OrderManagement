using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly OrderManagementDbContext _context;

    public ProductRepository(OrderManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product> CreateAsync(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<Product?> UpdateAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _context.Entry(product).State = EntityState.Modified;
        
        try
        {
            await _context.SaveChangesAsync();
            return product;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ProductExistsAsync(product.Id))
                return null;
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;

        // Soft delete - just mark as inactive
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> ProductExistsAsync(int id)
    {
        return await _context.Products.AnyAsync(e => e.Id == id);
    }
}
