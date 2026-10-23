
USE OrderingDb;
GO




-- ============================================================================
-- 1. НАПОВНЕННЯ ТАБЛИЦІ КЛІЄНТІВ (dbo.Customers)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Phone = '+380501112233')
BEGIN
INSERT INTO dbo.Customers (FullName, Phone)
VALUES (N'Олег Петренко', '+380501112233');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Phone = '+380672223344')
BEGIN
INSERT INTO dbo.Customers (FullName, Phone)
VALUES (N'Марія Коваль', '+380672223344');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Phone = '+380933334455')
BEGIN
INSERT INTO dbo.Customers (FullName, Phone)
VALUES (N'Дмитро Мельник', '+380933334455');
END;

-- ============================================================================
-- 2. НАПОВНЕННЯ ЛОКАЛЬНОГО ДОВІДНИКА СТРАВ (dbo.LocalMenuItems)
-- Ідентифікатори відповідають сутностям із сервісу Каталогу
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.LocalMenuItems WHERE Id = 101)
BEGIN
INSERT INTO dbo.LocalMenuItems (Id, Name, CurrentPrice, IsActive)
VALUES (101, N'Піца Дьявола 30см', 280.00, 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.LocalMenuItems WHERE Id = 102)
BEGIN
INSERT INTO dbo.LocalMenuItems (Id, Name, CurrentPrice, IsActive)
VALUES (102, N'Бургер Класичний', 195.00, 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.LocalMenuItems WHERE Id = 103)
BEGIN
INSERT INTO dbo.LocalMenuItems (Id, Name, CurrentPrice, IsActive)
VALUES (103, N'Салат Цезар з куркою', 165.00, 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.LocalMenuItems WHERE Id = 104)
BEGIN
INSERT INTO dbo.LocalMenuItems (Id, Name, CurrentPrice, IsActive)
VALUES (104, N'Лимонад цитрусовий 0.5л', 60.00, 1);
END;

-- ============================================================================
-- 3. ТЕСТОВІ ЗАМОВЛЕННЯ ДЛЯ ПЕРЕВІРКИ ЛОГІКИ ТА СТАТУСІВ
-- ============================================================================
DECLARE @CustomerId1 BIGINT = (SELECT TOP 1 Id FROM dbo.Customers WHERE Phone = '+380501112233');
DECLARE @CustomerId2 BIGINT = (SELECT TOP 1 Id FROM dbo.Customers WHERE Phone = '+380672223344');

-- Замовлення 1: Нове ('Submitted') — готове для тестування процедури підтвердження
IF NOT EXISTS (
    SELECT 1 FROM dbo.Orders o 
    JOIN dbo.OrderDetails d ON o.Id = d.OrderId 
    WHERE d.DeliveryAddress = N'вул. Степана Бандери, 12, кв. 4'
)
BEGIN
INSERT INTO dbo.Orders (CustomerId, Status, TotalAmount)
VALUES (@CustomerId1, 'Submitted', 540.00);

DECLARE @NewOrderId1 BIGINT = SCOPE_IDENTITY();

INSERT INTO dbo.OrderDetails (OrderId, DeliveryAddress, CourierInstructions)
VALUES (@NewOrderId1, N'вул. Степана Бандери, 12, кв. 4', N'Домофон не працює, наберіть за 5 хв');

INSERT INTO dbo.OrderItems (OrderId, MenuItemId, ItemName, UnitPrice, Quantity)
VALUES
    (@NewOrderId1, 101, N'Піца Дьявола 30см', 280.00, 1),
    (@NewOrderId1, 104, N'Лимонад цитрусовий 0.5л', 60.00, 2);
END;

-- Замовлення 2: Вже підтверджене ('Confirmed') — для тестування відмови повторного переходу
IF NOT EXISTS (
    SELECT 1 FROM dbo.Orders o 
    JOIN dbo.OrderDetails d ON o.Id = d.OrderId 
    WHERE d.DeliveryAddress = N'вул. Українська, 45'
)
BEGIN
INSERT INTO dbo.Orders (CustomerId, Status, TotalAmount)
VALUES (@CustomerId2, 'Confirmed', 195.00);

DECLARE @NewOrderId2 BIGINT = SCOPE_IDENTITY();

INSERT INTO dbo.OrderDetails (OrderId, DeliveryAddress, CourierInstructions)
VALUES (@NewOrderId2, N'вул. Українська, 45', N'Залишити біля дверей');

INSERT INTO dbo.OrderItems (OrderId, MenuItemId, ItemName, UnitPrice, Quantity)
VALUES (@NewOrderId2, 102, N'Бургер Класичний', 195.00, 1);
END;