# DeliverySystem Microservices

Цей репозиторій містить розробку мікросервісної архітектури системи доставки.

##  Лабораторна робота №1: Проєктування БД та політика дублювання

### Політика дублювання даних між мікросервісами
| Поле | Де зберігається (Копія) | Джерело-власник | Політика узгодженості |
| :--- | :--- | :--- | :--- |
| `ItemName`, `UnitPrice` | `OrderingDb` (таблиця `OrderItems`) | Мікросервіс `Catalog` | **Знімок (Snapshot):** Дані фіксуються під час створення замовлення. Якщо ціна чи назва в каталозі змінюються згодом, у вже створеному замовленні вони залишаються незмінними. |
| `ProductName` / `LocalMenuItems` | `ReviewsDb` | Мікросервіс `Catalog` | **Оновлення подіями (Eventual Consistency):** При зміні назви товару в каталозі публікується подія, яка асинхронно оновлює кешовану назву в базі відгуків. |
| `Status` / `OrderId` | `ReviewsDb` | Мікросервіс `Ordering` | **Оновлення подіями (Eventual Consistency):** Після успішного завершення замовлення публікується подія, що верифікує покупку і надає клієнту статус Verified Buyer для написання відгуку. |

## 🏗 Лабораторна робота №2: Тришарова архітектура (ADO.NET + Dapper)

Мікросервіс `Ordering` реалізовано як самостійну вертикаль (Vertical Slice) з використанням 3-tier архітектури.

### Структура проєкту
1. **Ordering.Api** (Web/API) — контролери, middleware, глобальна обробка помилок `ProblemDetails` (статуси 400, 404, 409).
2. **Ordering.Bll** (Business Logic Layer) — сервіси (`OrderService`), DTO, мапінг через `AutoMapper`.
3. **Ordering.Dal** (Data Access Layer) — інтерфейси репозиторіїв (`IGenericRepository<T>`, `IOrderRepository`, `IProductRepository`, `ICustomerRepository`), `UnitOfWork` та реалізації: `CustomerRepository` на чистому ADO.NET, `OrderRepository` і `ProductRepository` на Dapper зі спільною базою `BaseDapperRepository<T>`. Також `DatabaseInitializer`, який при старті створює схему, процедуру `usp_ConfirmOrder` і тестові дані.
4. **Ordering.Domain** — моделі (`Order`, `OrderItem`, `Customer`, `Product`) та доменні винятки (`NotFoundException`, `BusinessConflictException`, `ValidationException`).
5. **Ordering.AppHost** — проєкт `.NET Aspire` для єдиної точки запуску API та БД.

Залежності йдуть в один бік: `Api → Bll → Dal → Domain`.

### Вимоги
- .NET SDK 10
- Docker (Aspire запускає SQL Server у контейнері)

### Запуск через .NET Aspire
1. Відкрийте термінал у папці `OrderingContext`.
2. Виконайте команду:
   ```bash
   dotnet run --project Ordering.AppHost
   ```
3. Перейдіть за посиланням на Aspire Dashboard, яке з'явиться в консолі (зазвичай `https://localhost:17299`).
4. Дочекайтесь, поки ресурси `sql` та `orders-api` перейдуть у стан Running. API стартує після готовності БД й сам створює таблиці та тестові дані (клієнти 1 і 2, страви 101–104).
5. В Aspire Dashboard відкрийте ендпоінт `orders-api` та додайте `/swagger` до URL для доступу до OpenAPI.

Порт API призначає Aspire, тож у прикладах нижче підставте адресу ендпоінта `orders-api` з дашборду (`https://localhost:7106` — лише приклад). Ті самі запити є у файлі `Ordering.Api/Ordering.Api.http`.

### Приклади запитів (cURL)

**Створення замовлення (POST), відповідь `201 Created` із заголовком `Location`:**
```bash
curl -X POST 'https://localhost:7106/api/orders' \
  -H 'Content-Type: application/json' \
  -d '{
  "customerId": 1,
  "deliveryAddress": "вул. Центральна, 5, кв. 12",
  "courierInstructions": "Подзвонити за 5 хвилин",
  "items": [
    { "productId": 101, "units": 2 },
    { "productId": 104, "units": 1 }
  ]
}'
```

**Отримання замовлення (GET), `200 OK`:**
```bash
curl 'https://localhost:7106/api/orders/1'
```

**Підтвердження замовлення з транзакцією (POST), `204 No Content`:**
```bash
curl -X POST 'https://localhost:7106/api/orders/1/confirm'
```

### Обробка помилок (`ProblemDetails`)
| Ситуація | Статус |
| :--- | :--- |
| Некоректне тіло запиту (DataAnnotations), неіснуючий клієнт або страва, дублікати позицій | `400 Bad Request` |
| Замовлення не знайдено | `404 Not Found` |
| Повторне підтвердження або підтвердження замовлення не в статусі `Submitted` | `409 Conflict` |
| Непередбачена помилка | `500 Internal Server Error` |