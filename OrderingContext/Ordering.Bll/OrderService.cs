using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ordering.Dal;
using Ordering.Domain;

namespace Ordering.Bll;

public sealed class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public OrderService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<OrderDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var order = await _uow.Orders.GetWithItemsAsync(id, ct)
                    ?? throw new NotFoundException($"Order {id} not found");

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto input, CancellationToken ct = default)
    {
        var order = new Order { CustomerId = input.CustomerId };

        foreach (var item in input.Items)
        {
            var product = await _uow.Products.GetByIdAsync(item.ProductId, ct)
                          ?? throw new ValidationException($"Product {item.ProductId} not found");

            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Units = item.Units
            });
        }

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var orderId = await _uow.Orders.AddAsync(order, ct);
            order.Id = orderId;
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        return _mapper.Map<OrderDto>(order);
    }

    public async Task ConfirmOrderAsync(long orderId, CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var order = await _uow.Orders.GetWithItemsAsync(orderId, ct)
                        ?? throw new NotFoundException($"Order {orderId} not found");

            if (order.Items.Count == 0)
                throw new BusinessConflictException($"Order {orderId} has no items");

            await _uow.Orders.ConfirmAsync(orderId, ct);

            foreach (var item in order.Items)
                await _uow.Products.DecreaseStockAsync(item.ProductId, item.Units, ct);

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}