namespace OrderManagement.Application.DTOs;


public class OrderStatusBreakdown
{
    public int PendingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
    
    public decimal PendingPercentage { get; set; }
    public decimal ShippedPercentage { get; set; }
    public decimal DeliveredPercentage { get; set; }
    public decimal CancelledPercentage { get; set; }
}


