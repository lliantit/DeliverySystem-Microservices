-- 1. Таблиця Клієнтів
CREATE TABLE dbo.Customers (
                               Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                               FullName NVARCHAR(150) NOT NULL,
                               Phone NVARCHAR(20) NOT NULL UNIQUE,
                               CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                               IsDeleted BIT NOT NULL DEFAULT 0
);

-- 2. Локальна копія довідника страв (щоб уникнути FOREIGN KEY до іншої БД)
CREATE TABLE dbo.LocalMenuItems (
                                    Id BIGINT PRIMARY KEY, -- Id приходить з БД Каталогу
                                    Name NVARCHAR(200) NOT NULL,
                                    CurrentPrice DECIMAL(18,2) NOT NULL CHECK (CurrentPrice >= 0),
                                    IsActive BIT NOT NULL DEFAULT 1
);

-- 3. Таблиця Замовлень (Зв'язок 1:N від Customers)
CREATE TABLE dbo.Orders (
                            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                            CustomerId BIGINT NOT NULL REFERENCES dbo.Customers(Id),
                            Status NVARCHAR(30) NOT NULL DEFAULT 'Submitted' 
        CHECK (Status IN ('Submitted', 'Confirmed', 'Preparing', 'Shipped', 'Delivered', 'Cancelled')),
                            TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (TotalAmount >= 0),
                            CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                            UpdatedAt DATETIME2 NULL,
                            IsDeleted BIT NOT NULL DEFAULT 0,
                            RowVersion ROWVERSION -- Оптимістична конкурентність
);

-- 4. Деталі замовлення (Зв'язок 1:1 з Orders)
-- Зовнішній ключ є одночасно первинним ключем
CREATE TABLE dbo.OrderDetails (
                                  OrderId BIGINT PRIMARY KEY REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
                                  DeliveryAddress NVARCHAR(500) NOT NULL,
                                  CourierInstructions NVARCHAR(1000) NULL
);

-- 5. Позиції замовлення (Зв'язок M:N між Orders та LocalMenuItems)
CREATE TABLE dbo.OrderItems (
                                OrderId BIGINT NOT NULL REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
                                MenuItemId BIGINT NOT NULL REFERENCES dbo.LocalMenuItems(Id),
                                ItemName NVARCHAR(200) NOT NULL, -- Знімок назви (ніколи не оновлюється)
                                UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0), -- Знімок ціни
                                Quantity INT NOT NULL CHECK (Quantity > 0),
                                PRIMARY KEY (OrderId, MenuItemId)
);



-- Індекси для оптимізації частих запитів
-- Покриваючий індекс для пошуку замовлень конкретного клієнта за статусом
CREATE INDEX IX_Orders_CustomerId_Status ON dbo.Orders (CustomerId, Status);
-- Фільтрований індекс, який ігнорує видалені записи
CREATE INDEX IX_Orders_Active ON dbo.Orders (CustomerId) WHERE IsDeleted = 0;
