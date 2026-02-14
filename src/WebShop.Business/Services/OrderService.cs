using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Business.Helpers;
using WebShop.Business.Models;
using WebShop.Business.Services.Base;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for order business operations.
/// </summary>
public class OrderService(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IAddressRepository addressRepository,
    IUnitOfWork unitOfWork,
    ILogger<OrderService> logger)
    : CrudServiceBase<Order, OrderDto, CreateOrderDto, UpdateOrderDto>(orderRepository, unitOfWork, logger),
        Interfaces.IOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly IAddressRepository _addressRepository = addressRepository;

    /// <inheritdoc />
    protected override string EntityName => "Order";

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        List<Order> orders = await _orderRepository.GetByCustomerIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        return orders.Adapt<IReadOnlyList<OrderDto>>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        List<Order> orders = await _orderRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);
        return orders.Adapt<IReadOnlyList<OrderDto>>();
    }

    /// <inheritdoc />
    public override async Task<OrderDto> CreateAsync(CreateOrderDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        Customer? customer = await _customerRepository.GetByIdAsync(createDto.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer == null)
        {
            Logger.LogWarning("Order creation failed: Customer not found. CustomerId: {CustomerId}", createDto.CustomerId);
            throw new ArgumentException($"Customer with ID {createDto.CustomerId} not found.", nameof(createDto));
        }

        Address? address = await _addressRepository.GetByIdAsync(createDto.ShippingAddressId, cancellationToken).ConfigureAwait(false);
        if (address == null)
        {
            Logger.LogWarning("Order creation failed: Shipping address not found. ShippingAddressId: {ShippingAddressId}", createDto.ShippingAddressId);
            throw new ArgumentException($"Shipping address with ID {createDto.ShippingAddressId} not found.", nameof(createDto));
        }

        if (address.CustomerId != createDto.CustomerId)
        {
            Logger.LogWarning("Order creation failed: Shipping address does not belong to customer. ShippingAddressId: {ShippingAddressId}, CustomerId: {CustomerId}, AddressCustomerId: {AddressCustomerId}",
                createDto.ShippingAddressId, createDto.CustomerId, address.CustomerId);
            throw new ArgumentException($"Shipping address {createDto.ShippingAddressId} does not belong to customer {createDto.CustomerId}.", nameof(createDto));
        }

        Logger.LogInformation("Creating new order. CustomerId: {CustomerId}, ShippingAddressId: {ShippingAddressId}", createDto.CustomerId, createDto.ShippingAddressId);
        Order order = createDto.Adapt<Order>();
        order.OrderTimestamp = DateTime.UtcNow;
        await Repository.AddAsync(order, cancellationToken).ConfigureAwait(false);
        await Repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("Order created successfully. OrderId: {OrderId}", order.Id);
        return order.Adapt<OrderDto>();
    }

    /// <inheritdoc />
    protected override bool ApplyPatch(Order entity, UpdateOrderDto patchDto)
    {
        return PartialUpdateHelper.ApplyIfChanged(entity.CustomerId, patchDto.CustomerId, v => entity.CustomerId = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.OrderTimestamp, patchDto.OrderTimestamp, v => entity.OrderTimestamp = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.ShippingAddressId, patchDto.ShippingAddressId, v => entity.ShippingAddressId = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Total, patchDto.Total, v => entity.Total = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.ShippingCost, patchDto.ShippingCost, v => entity.ShippingCost = v);
    }
}
