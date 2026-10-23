using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Ordering.Dal;

public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    protected readonly string TableName;

    protected BaseDapperRepository(SqlConnection connection, Func<SqlTransaction?> transaction, string tableName)
        : base(connection, transaction)
    {
        TableName = tableName;
    }

    public virtual async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id";
        
        return await Connection.QuerySingleOrDefaultAsync<T>(new CommandDefinition(
            sql,
            new { Id = id },
            Transaction,
            cancellationToken: ct));
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var sql = $"SELECT * FROM {TableName}";
        
        var result = await Connection.QueryAsync<T>(new CommandDefinition(
            sql,
            transaction: Transaction,
            cancellationToken: ct));
            
        return result.ToList();
    }

    public virtual async Task<long> AddAsync(T entity, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);

        // Фільтруємо Id, RowVersion та навігаційні колекції/об'єкти
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)
                     && !p.Name.Equals("RowVersion", StringComparison.OrdinalIgnoreCase)
                     && (!p.PropertyType.IsClass || p.PropertyType == typeof(string)))
            .ToList();

        var columns = string.Join(", ", props.Select(p => p.Name));
        var parameters = string.Join(", ", props.Select(p => "@" + p.Name));

        var sql = $"INSERT INTO {TableName} ({columns}) VALUES ({parameters}); SELECT CAST(COALESCE(SCOPE_IDENTITY(), 0) AS BIGINT);";

        var id = await Connection.ExecuteScalarAsync<long?>(new CommandDefinition(
            sql,
            entity,
            Transaction,
            cancellationToken: ct));

        if (id.HasValue && id.Value > 0)
        {
            return id.Value;
        }

        var idProp = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        if (idProp != null && idProp.GetValue(entity) is long explicitId)
        {
            return explicitId;
        }

        return 0;
    }

    public virtual async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var sql = $"DELETE FROM {TableName} WHERE Id = @Id";
        
        var affected = await Connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id },
            Transaction,
            cancellationToken: ct));
            
        return affected > 0;
    }
}