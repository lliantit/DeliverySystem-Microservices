using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Ordering.Domain;

namespace Ordering.Dal;

public sealed class OrderRepository : BaseDapperRepository<Order>, IOrderRepository
{
    public OrderRepository(SqlConnection c, Func<SqlTransaction?> tx)
        : base(c, tx, "dbo.Orders") { }

    public async Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default)
    {
        // Використовуємо AS, щоб підлаштувати колонки БД під C# властивості
        const string sql = @"
            SELECT o.Id, o.CustomerId, o.Status, o.CreatedAt AS OrderDate,
                   i.OrderId, i.MenuItemId AS ProductId, i.ItemName AS ProductName, i.UnitPrice, i.Quantity AS Units
            FROM dbo.Orders o 
            LEFT JOIN dbo.OrderItems i ON i.OrderId = o.Id
            WHERE o.Id = @OrderId";

        Order? result = null;

        await Connection.QueryAsync<Order, OrderItem?, Order>(
            new CommandDefinition(sql, new { OrderId = orderId }, Transaction, cancellationToken: ct),
            (order, item) =>
            {
                result ??= order;
                if (result.Items == null) result.Items = new();
                if (item is not null) result.Items.Add(item);
                return result;
            },
            splitOn: "OrderId"); 

        return result;
    }

    public Task ConfirmAsync(long orderId, CancellationToken ct = default)
    {
        return Connection.ExecuteAsync(new CommandDefinition(
            "dbo.usp_ConfirmOrder", 
            new { OrderId = orderId }, 
            Transaction,
            commandType: CommandType.StoredProcedure, 
            cancellationToken: ct));
    }
}