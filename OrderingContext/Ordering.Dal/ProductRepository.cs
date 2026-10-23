using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Ordering.Domain;

namespace Ordering.Dal;

public interface IProductRepository : IGenericRepository<Product>
{
    Task DecreaseStockAsync(long productId, int quantity, CancellationToken ct = default);
}

public sealed class ProductRepository : BaseDapperRepository<Product>, IProductRepository
{
    public ProductRepository(SqlConnection c, Func<SqlTransaction?> tx) : base(c, tx, "dbo.LocalMenuItems") { }

    // Колонка CurrentPrice мапиться на Product.Price через аліас; неактивні страви не повертаються.
    public override async Task<Product?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        const string sql = @"
            SELECT Id, Name, CurrentPrice AS Price
            FROM dbo.LocalMenuItems
            WHERE Id = @Id AND IsActive = 1";

        return await Connection.QuerySingleOrDefaultAsync<Product>(new CommandDefinition(
            sql, new { Id = id }, Transaction, cancellationToken: ct));
    }

    public async Task DecreaseStockAsync(long productId, int quantity, CancellationToken ct = default)
    {
        // У локальному довіднику немає залишків (Stock): облік залишків належить сервісу Каталогу.
        // Тому в межах транзакції підтвердження лише блокуємо рядок і перевіряємо, що страва
        // досі активна; якщо її деактивували між створенням і підтвердженням — це конфлікт.
        await EnsureOpenAsync(ct);

        const string sql = @"
            UPDATE dbo.LocalMenuItems
            SET IsActive = IsActive
            WHERE Id = @ProductId AND IsActive = 1";

        var affected = await Connection.ExecuteAsync(new CommandDefinition(
            sql, new { ProductId = productId }, Transaction, cancellationToken: ct));

        if (affected == 0)
            throw new BusinessConflictException($"Product {productId} is no longer available");
    }
}
