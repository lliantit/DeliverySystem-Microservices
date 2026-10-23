using System;
using System.Collections.Generic;

namespace Ordering.Domain;

public class Order
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Status { get; set; } = "Submitted";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    // Дані доставки зберігаються в dbo.OrderDetails (зв'язок 1:1)
    public string DeliveryAddress { get; set; } = "";
    public string? CourierInstructions { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
