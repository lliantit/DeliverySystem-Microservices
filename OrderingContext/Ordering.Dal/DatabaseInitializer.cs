using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Ordering.Dal;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(string connectionString)
    {
        for (int i = 0; i < 30; i++)
        {
            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                const string checkSql = "SELECT OBJECT_ID('dbo.Orders', 'U')";
                await using var checkCmd = new SqlCommand(checkSql, conn);
                var exists = await checkCmd.ExecuteScalarAsync();

                if (exists == null || exists == DBNull.Value)
                {
                    const string ddl = @"
                        IF OBJECT_ID('dbo.Customers', 'U') IS NULL
                        CREATE TABLE dbo.Customers (
                            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                            FullName NVARCHAR(150) NOT NULL,
                            Email NVARCHAR(100) NOT NULL,
                            CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                            IsDeleted BIT NOT NULL DEFAULT 0
                        );

                        IF OBJECT_ID('dbo.LocalMenuItems', 'U') IS NULL
                        CREATE TABLE dbo.LocalMenuItems (
                            Id BIGINT PRIMARY KEY,
                            Name NVARCHAR(200) NOT NULL,
                            CurrentPrice DECIMAL(18,2) NOT NULL CHECK (CurrentPrice >= 0),
                            IsActive BIT NOT NULL DEFAULT 1
                        );

                        IF OBJECT_ID('dbo.Orders', 'U') IS NULL
                        CREATE TABLE dbo.Orders (
                            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                            CustomerId BIGINT NOT NULL REFERENCES dbo.Customers(Id),
                            Status NVARCHAR(30) NOT NULL DEFAULT 'Submitted',
                            TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                            CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                            UpdatedAt DATETIME2 NULL,
                            IsDeleted BIT NOT NULL DEFAULT 0,
                            RowVersion ROWVERSION
                        );

                        IF OBJECT_ID('dbo.OrderDetails', 'U') IS NULL
                        CREATE TABLE dbo.OrderDetails (
                            OrderId BIGINT PRIMARY KEY REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
                            DeliveryAddress NVARCHAR(500) NOT NULL,
                            CourierInstructions NVARCHAR(1000) NULL
                        );

                        IF OBJECT_ID('dbo.OrderItems', 'U') IS NULL
                        CREATE TABLE dbo.OrderItems (
                            OrderId BIGINT NOT NULL REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
                            MenuItemId BIGINT NOT NULL REFERENCES dbo.LocalMenuItems(Id),
                            ItemName NVARCHAR(200) NOT NULL,
                            UnitPrice DECIMAL(18,2) NOT NULL,
                            Quantity INT NOT NULL,
                            PRIMARY KEY (OrderId, MenuItemId)
                        );
                    ";

                    await using var ddlCmd = new SqlCommand(ddl, conn);
                    await ddlCmd.ExecuteNonQueryAsync();
                }

                break;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }
    }
}