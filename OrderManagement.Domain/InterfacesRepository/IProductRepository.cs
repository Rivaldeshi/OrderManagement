using OrderManagement.Domain.Entities;

namespace OrderManagement.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product> CreateAsync(Product product);
    Task<Product?> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
}