using System.Threading;
using System.Threading.Tasks;
using Ordering.Domain;

namespace Ordering.Dal;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default);
    Task ConfirmAsync(long orderId, CancellationToken ct = default);
}