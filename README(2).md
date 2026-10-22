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
3. **Ordering.Dal** (Data Access Layer) — `UnitOfWork` та репозиторії (`CustomerRepository` на ADO.NET, `OrderRepository` та `ProductRepository` на Dapper).
4. **Ordering.Domain** — спільні моделі даних, інтерфейси репозиторіїв та доменні винятки.
5. **Ordering.AppHost** — проєкт `.NET Aspire` для єдиної точки запуску API та БД.

###  Запуск через .NET Aspire
1. Відкрийте термінал у корені рішення (`DeliverySystem/OrderingContext`).
2. Виконайте команду:
   ```bash
   dotnet run --project Ordering.AppHost
   ```
3. Перейдіть за посиланням на Aspire Dashboard, яке з'явиться в консолі (зазвичай `https://localhost:17299`).
4. В Aspire Dashboard відкрийте ендпоінт `orders-api` та додайте `/swagger` до URL для доступу до OpenAPI.

###  Приклади запитів (cURL)

**Створення замовлення (POST):**
```bash
curl -X 'POST' \
  'https://localhost:7106/api/orders' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
  "customerId": 1,
  "items": [
    {
      "productId": 1,
      "units": 2
    }
  ]
}'
```

**Отримання замовлення (GET):**
```bash
curl -X 'GET' \
  'https://localhost:7106/api/orders/1' \
  -H 'accept: text/plain'
```

**Підтвердження замовлення з транзакцією (POST):**
```bash
curl -X 'POST' \
  'https://localhost:7106/api/orders/1/confirm' \
  -H 'accept: */*' \
  -d ''
```