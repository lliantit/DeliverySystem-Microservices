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
    public ProductRepository(SqlConnection c, Func<SqlTransaction?> tx) : base(c, tx, "dbo.Products") { }

    public Task DecreaseStockAsync(long productId, int quantity, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.Products SET Stock = Stock - @Quantity WHERE Id = @ProductId";
        return Connection.ExecuteAsync(new CommandDefinition(
            sql, 
            new { Quantity = quantity, ProductId = productId }, 
            Transaction, 
            cancellationToken: ct));
    }
}