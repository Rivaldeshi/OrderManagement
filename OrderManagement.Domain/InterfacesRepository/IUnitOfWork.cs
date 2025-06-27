namespace OrderManagement.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ICustomerRepository Customers { get; }
    IOrderRepository Orders { get; }
    IProductRepository Products { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}