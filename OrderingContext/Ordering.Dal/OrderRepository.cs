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
        const string sql = @"
            SELECT o.Id, o.CustomerId, o.Status, o.OrderDate,
                   i.OrderId, i.ProductId, i.ProductName, i.UnitPrice, i.Units
            FROM dbo.Orders o 
            LEFT JOIN dbo.OrderItems i ON i.OrderId = o.Id
            WHERE o.Id = @OrderId";

        await EnsureOpenAsync(ct);
        Order? result = null;

        await Connection.QueryAsync<Order, OrderItem?, Order>(
            new CommandDefinition(sql, new { OrderId = orderId }, Transaction, cancellationToken: ct),
            (order, item) =>
            {
                result ??= order;
                if (item is not null) result.Items.Add(item);
                return result;
            },
            splitOn: "OrderId"); // Dapper розуміє, що з цього стовпця починаються дані OrderItem

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