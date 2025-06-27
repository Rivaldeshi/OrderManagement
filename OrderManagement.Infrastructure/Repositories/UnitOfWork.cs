using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly OrderManagementDbContext _context;

    public UnitOfWork(OrderManagementDbContext context)
    {
        _context = context;
        Customers = new CustomerRepository(_context);
        Orders = new OrderRepository(_context);
        Products = new ProductRepository(_context);
    }

    public ICustomerRepository Customers { get; }
    public IOrderRepository Orders { get; }
    public IProductRepository Products { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}