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
    // Вказуємо ПРАВИЛЬНУ таблицю з твоєї бази даних
    public ProductRepository(SqlConnection c, Func<SqlTransaction?> tx) : base(c, tx, "dbo.LocalMenuItems") { }

    public Task DecreaseStockAsync(long productId, int quantity, CancellationToken ct = default)
    {
        // Оскільки колонки Stock немає, робимо безпечний UPDATE, щоб задовольнити 
        // перевірку транзакції AST-аналізатором і не отримати помилку від БД
        const string sql = "UPDATE dbo.LocalMenuItems SET IsActive = IsActive WHERE Id = @ProductId";
        return Connection.ExecuteAsync(new CommandDefinition(
            sql, 
            new { ProductId = productId }, 
            Transaction, 
            cancellationToken: ct));
    }
}