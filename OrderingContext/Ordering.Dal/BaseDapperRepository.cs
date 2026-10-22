using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Ordering.Dal;

public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    protected readonly string TableName;

    protected BaseDapperRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction, string tableName)
        : base(connection, currentTransaction)
    {
        TableName = tableName;
    }

    // Заглушки для базового CRUD (ми розширимо їх пізніше за потреби)
    public virtual Task<T?> GetByIdAsync(long id, CancellationToken ct = default) => throw new NotImplementedException();
    public virtual Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
    public virtual Task<long> AddAsync(T entity, CancellationToken ct = default) => throw new NotImplementedException();
    public virtual Task<bool> DeleteAsync(long id, CancellationToken ct = default) => throw new NotImplementedException();
}