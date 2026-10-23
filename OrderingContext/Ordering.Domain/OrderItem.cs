namespace Ordering.Domain;

public class OrderItem
{
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Units { get; set; }
}