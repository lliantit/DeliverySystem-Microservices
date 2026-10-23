using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Ordering.Dal;

/// <summary>
/// Створює схему, збережену процедуру та мінімальні тестові дані при старті API
/// (SQL Server у контейнері Aspire стартує порожнім). Усі кроки ідемпотентні.
/// </summary>
public static class DatabaseInitializer
{
    private const int MaxAttempts = 30;

    private const string SchemaSql = """
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
            Status NVARCHAR(30) NOT NULL DEFAULT 'Submitted'
                CHECK (Status IN ('Submitted', 'Confirmed', 'Preparing', 'Shipped', 'Delivered', 'Cancelled')),
            TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (TotalAmount >= 0),
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
            UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0),
            Quantity INT NOT NULL CHECK (Quantity > 0),
            PRIMARY KEY (OrderId, MenuItemId)
        );
        """;

    // CREATE OR ALTER PROCEDURE має бути єдиною інструкцією в пакеті, тому виконується окремою командою.
    private const string ConfirmOrderProcedureSql = """
        CREATE OR ALTER PROCEDURE dbo.usp_ConfirmOrder
            @OrderId BIGINT
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON; -- будь-яка помилка відкочує транзакцію

            BEGIN TRANSACTION;

            DECLARE @Status NVARCHAR(30) =
                (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId AND IsDeleted = 0);

            IF @Status IS NULL
                THROW 50001, N'Замовлення не знайдено', 1;

            IF @Status <> 'Submitted'
                THROW 50002, N'Підтвердити можна лише нове замовлення (Submitted)', 1;

            UPDATE dbo.Orders
            SET Status = 'Confirmed', UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @OrderId;

            COMMIT TRANSACTION;
        END;
        """;

    // Мінімальні дані, щоб приклади з README працювали одразу після запуску.
    private const string SeedSql = """
        IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
            INSERT INTO dbo.Customers (FullName, Email)
            VALUES (N'Олег Петренко', N'oleg.petrenko@example.com'),
                   (N'Марія Коваль', N'maria.koval@example.com');

        IF NOT EXISTS (SELECT 1 FROM dbo.LocalMenuItems)
            INSERT INTO dbo.LocalMenuItems (Id, Name, CurrentPrice, IsActive)
            VALUES (101, N'Піца Папероні 30см', 280.00, 1),
                   (102, N'Бургер Класичний', 195.00, 1),
                   (103, N'Салат Цезар з куркою', 165.00, 1),
                   (104, N'Лимонад цитрусовий 0.5л', 60.00, 1);
        """;

    public static async Task InitializeAsync(string connectionString, CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(ct);

                await ExecuteAsync(conn, SchemaSql, ct);
                await ExecuteAsync(conn, ConfirmOrderProcedureSql, ct);
                await ExecuteAsync(conn, SeedSql, ct);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && ex is SqlException or InvalidOperationException)
            {
                // Контейнер SQL Server ще стартує — чекаємо й пробуємо знову.
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }
    }

    private static async Task ExecuteAsync(SqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
