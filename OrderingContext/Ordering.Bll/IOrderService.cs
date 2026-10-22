using System.Threading;
using System.Threading.Tasks;

namespace Ordering.Bll;

public interface IOrderService
{
    Task<OrderDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<OrderDto> CreateAsync(CreateOrderDto input, CancellationToken ct = default);
    Task ConfirmOrderAsync(long orderId, CancellationToken ct = default);
}