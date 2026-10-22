using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ordering.Bll;

namespace Ordering.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service) => _service = service;

    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrderDto>> GetById(long id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:long}/confirm")]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        await _service.ConfirmOrderAsync(id, ct);
        return NoContent(); // Повертає 204
    }
}