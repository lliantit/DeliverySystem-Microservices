var builder = DistributedApplication.CreateBuilder(args);

// 1. Створюємо контейнер із SQL Server та базу даних
var db = builder.AddSqlServer("sql")
    .AddDatabase("OrdersDb");

// 2. Запускаємо наш API і передаємо йому рядок підключення до БД
builder.AddProject<Projects.Ordering_Api>("orders-api")
    .WithReference(db);

builder.Build().Run();