using OrderManagement.Domain.Entities;

namespace OrderManagement.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<IEnumerable<Order>> GetAllAsync();
    Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId);
    Task<Order> CreateAsync(Order order);
    Task<Order?> UpdateAsync(Order order);
    Task<bool> DeleteAsync(int id);
}