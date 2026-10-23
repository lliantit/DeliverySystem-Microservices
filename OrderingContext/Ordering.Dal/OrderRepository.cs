using System;
using System.Data;
using System.Linq;
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

    // Замовлення складається з трьох таблиць (Orders, OrderDetails, OrderItems),
    // тому універсальний INSERT з бази тут не підходить.
    public override async Task<long> AddAsync(Order order, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        var total = order.Items.Sum(i => i.UnitPrice * i.Units);

        var id = await Connection.ExecuteScalarAsync<long>(new CommandDefinition(
            @"INSERT INTO dbo.Orders (CustomerId, Status, TotalAmount)
              VALUES (@CustomerId, 'Submitted', @Total);
              SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
            new { order.CustomerId, Total = total },
            Transaction,
            cancellationToken: ct));

        await Connection.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO dbo.OrderDetails (OrderId, DeliveryAddress, CourierInstructions)
              VALUES (@OrderId, @DeliveryAddress, @CourierInstructions)",
            new { OrderId = id, order.DeliveryAddress, order.CourierInstructions },
            Transaction,
            cancellationToken: ct));

        await Connection.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO dbo.OrderItems (OrderId, MenuItemId, ItemName, UnitPrice, Quantity)
              VALUES (@OrderId, @ProductId, @ProductName, @UnitPrice, @Units)",
            order.Items.Select(i => new
            {
                OrderId = id,
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Units
            }),
            Transaction,
            cancellationToken: ct));

        return id;
    }

    public async Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default)
    {
        // Використовуємо AS, щоб підлаштувати колонки БД під C# властивості
        const string sql = @"
            SELECT o.Id, o.CustomerId, o.Status, o.CreatedAt AS OrderDate,
                   i.OrderId, i.MenuItemId AS ProductId, i.ItemName AS ProductName, i.UnitPrice, i.Quantity AS Units
            FROM dbo.Orders o
            LEFT JOIN dbo.OrderItems i ON i.OrderId = o.Id
            WHERE o.Id = @OrderId AND o.IsDeleted = 0";

        Order? result = null;

        await Connection.QueryAsync<Order, OrderItem?, Order>(
            new CommandDefinition(sql, new { OrderId = orderId }, Transaction, cancellationToken: ct),
            (order, item) =>
            {
                result ??= order;
                if (item is not null) result.Items.Add(item);
                return result;
            },
            splitOn: "OrderId");

        return result;
    }

    public async Task ConfirmAsync(long orderId, CancellationToken ct = default)
    {
        try
        {
            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_ConfirmOrder",
                new { OrderId = orderId },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        }
        // Коди з THROW у dbo.usp_ConfirmOrder перетворюємо на доменні винятки,
        // щоб глобальний обробник повернув 404/409, а не 500.
        catch (SqlException ex) when (ex.Number == 50001)
        {
            throw new NotFoundException($"Order {orderId} not found");
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            throw new BusinessConflictException($"Order {orderId} cannot be confirmed: only Submitted orders can be confirmed");
        }
    }
}
