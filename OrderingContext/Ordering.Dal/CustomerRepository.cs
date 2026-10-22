using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Ordering.Domain;

namespace Ordering.Dal;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default);
}

public sealed class CustomerRepository : RepositoryBase, ICustomerRepository
{
    public CustomerRepository(SqlConnection c, Func<SqlTransaction?> tx) : base(c, tx) { }

    public async Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Email, FullName FROM dbo.Customers WHERE Id = @Id";
        await EnsureOpenAsync(ct);
        
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;
        
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        
        return new Customer
        {
            Id = reader.GetInt64(0),
            Email = reader.GetString(1),
            FullName = reader.GetString(2)
        };
    }
}