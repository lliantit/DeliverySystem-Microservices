using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Ordering.Dal;

public interface IUnitOfWork : IAsyncDisposable
{
    ICustomerRepository Customers { get; }
    IOrderRepository Orders { get; }
    IProductRepository Products { get; }
    
    Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}