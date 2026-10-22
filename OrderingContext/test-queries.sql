


DECLARE @CreatedOrderId BIGINT;
EXEC dbo.usp_CreateOrder 
    @CustomerId = 1, 
    @DeliveryAddress = N'вул. Центральна, 5', 
    @CourierInstructions = N'Піднятися на 3 поверх',
    @OrderId = @CreatedOrderId OUTPUT;

SELECT @CreatedOrderId AS CreatedOrderId;

-- Перевірка створеного замовлення через процедуру читання
EXEC dbo.usp_GetOrder @OrderId = @CreatedOrderId;