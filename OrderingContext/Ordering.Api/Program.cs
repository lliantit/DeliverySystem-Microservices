using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ordering.Api;
using Ordering.Bll;
using Ordering.Dal;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Рядок підключення
var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
                       ?? throw new InvalidOperationException("Connection string 'OrdersDb' is missing");

// 2. Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// 3. Реєстрація сервісів
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IOrderService, OrderService>();

// 4. AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<OrderProfile>();
});

// 5. Глобальна обробка помилок
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// 6. Контролери та Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 7. Безпечне створення таблиць перед запуском
await EnsureDatabaseInitializedAsync(connectionString);

// 8. Middleware
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task EnsureDatabaseInitializedAsync(string connString)
{
    for (int i = 0; i < 40; i++)
    {
        try
        {
            await using var conn = new SqlConnection(connString);
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
                        Phone NVARCHAR(20) NOT NULL UNIQUE,
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
            await Task.Delay(1500);
        }
    }
}