using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using Serilog;
using Ordering.Bll;
using Ordering.Dal;
using Ordering.Api;

var builder = WebApplication.CreateBuilder(args);

// 1. Зчитуємо рядок підключення
var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
                       ?? throw new InvalidOperationException("Connection string 'OrdersDb' is missing");

// 2. Налаштовуємо Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// 3. Реєструємо UnitOfWork та сервіс бізнес-логіки
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IOrderService, OrderService>();

// 4. Реєструємо AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<OrderProfile>();
});

// 5. Налаштовуємо глобальну обробку помилок (ProblemDetails)
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// 6. Реєструємо контролери та Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 7. Включаємо middleware для обробки помилок
app.UseExceptionHandler();

// 8. Включаємо Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();