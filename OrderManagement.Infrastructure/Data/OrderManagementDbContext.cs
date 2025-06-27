using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Data;

public class OrderManagementDbContext : DbContext
{
    public OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Segment).HasConversion<int>().IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // One-to-many relationship: Customer -> Orders
            entity.HasMany(c => c.Orders)
                .WithOne(o => o.Customer)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Stock).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.SKU).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<int>().IsRequired();
            entity.Property(e => e.OrderDate).IsRequired();
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ShippingAddress).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();

            // One-to-many relationship: Order -> OrderItems
            entity.HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).IsRequired();

            // Many-to-one relationship: OrderItem -> Product
            entity.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}





