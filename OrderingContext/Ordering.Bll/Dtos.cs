using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ordering.Bll;

public record OrderItemDto(long ProductId, string ProductName, decimal UnitPrice, int Units);

public record OrderDto(long Id, long CustomerId, string Status, DateTime OrderDate, IReadOnlyList<OrderItemDto> Items);

public class CreateOrderDto
{
    [Range(1, long.MaxValue)]
    public long CustomerId { get; set; }

    [Required, MinLength(1)]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; set; }

    [Range(1, 1000)]
    public int Units { get; set; }
}