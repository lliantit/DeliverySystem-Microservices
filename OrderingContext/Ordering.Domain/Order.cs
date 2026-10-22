using System;
using System.Collections.Generic;

namespace Ordering.Domain;

public class Order
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    
    public List<OrderItem> Items { get; set; } = new();
}