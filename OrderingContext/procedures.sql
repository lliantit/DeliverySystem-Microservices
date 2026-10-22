USE OrderingDb;
GO









-- 1. Створення замовлення (CRUD: Create)
CREATE OR ALTER PROCEDURE dbo.usp_CreateOrder
    @CustomerId BIGINT,
    @DeliveryAddress NVARCHAR(500),
    @CourierInstructions NVARCHAR(1000) = NULL,
    @OrderId BIGINT OUTPUT
    AS
BEGIN
    SET NOCOUNT ON;
BEGIN TRY
BEGIN TRANSACTION;

INSERT INTO dbo.Orders (CustomerId) VALUES (@CustomerId);
SET @OrderId = SCOPE_IDENTITY();

INSERT INTO dbo.OrderDetails (OrderId, DeliveryAddress, CourierInstructions)
VALUES (@OrderId, @DeliveryAddress, @CourierInstructions);

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
END CATCH
END;
GO

-- 2. Отримання базової інформації про замовлення (CRUD: Read)
CREATE OR ALTER PROCEDURE dbo.usp_GetOrder
    @OrderId BIGINT
    AS
BEGIN
    SET NOCOUNT ON;
SELECT o.Id, o.Status, o.TotalAmount, d.DeliveryAddress, d.CourierInstructions
FROM dbo.Orders o
         JOIN dbo.OrderDetails d ON o.Id = d.OrderId
WHERE o.Id = @OrderId AND o.IsDeleted = 0;
END;
GO

-- 3. Транзакційна зміна статусу (Бізнес-логіка з перевіркою)
-- Використовуємо UPDLOCK, щоб ніхто інший не міг змінити статус під час перевірки
CREATE OR ALTER PROCEDURE dbo.usp_ConfirmOrder
    @OrderId BIGINT
    AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON; -- Будь-яка помилка автоматично відкочує транзакцію

BEGIN TRANSACTION;
    
    DECLARE @Status NVARCHAR(30) = (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId);
    
    IF @Status IS NULL
        THROW 50001, 'Замовлення не знайдено', 1;
        
    IF @Status <> 'Submitted'
        THROW 50002, 'Підтвердити можна лише нове замовлення (Submitted)', 1;

UPDATE dbo.Orders
SET Status = 'Confirmed', UpdatedAt = SYSUTCDATETIME()
WHERE Id = @OrderId;

COMMIT TRANSACTION;
END;
GO

-- 4. Скасування замовлення
CREATE OR ALTER PROCEDURE dbo.usp_CancelOrder
    @OrderId BIGINT
    AS
BEGIN
    SET NOCOUNT ON;
    -- Скасувати можна лише до того, як його почали готувати
UPDATE dbo.Orders
SET Status = 'Cancelled', UpdatedAt = SYSUTCDATETIME()
WHERE Id = @OrderId AND Status IN ('Submitted', 'Confirmed');
END;
GO