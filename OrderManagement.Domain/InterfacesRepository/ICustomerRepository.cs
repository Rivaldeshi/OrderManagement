using OrderManagement.Domain.Entities;

namespace OrderManagement.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer?> UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(int id);
}