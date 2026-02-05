using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for order business operations.
/// </summary>
public class OrderService(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IAddressRepository addressRepository,
    ILogger<OrderService> logger) : Interfaces.IOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository
        ?? throw new ArgumentNullException(nameof(orderRepository));
    private readonly ICustomerRepository _customerRepository = customerRepository
        ?? throw new ArgumentNullException(nameof(customerRepository));
    private readonly IAddressRepository _addressRepository = addressRepository
        ?? throw new ArgumentNullException(nameof(addressRepository));
    private readonly ILogger<OrderService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Order? order = await _orderRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return order?.Adapt<OrderDto>();
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Order> orders = await _orderRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return orders.Adapt<IReadOnlyList<OrderDto>>();
    }

    public async Task<(IReadOnlyList<OrderDto> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<Order> items, int totalCount) = await _orderRepository
            .GetPagedAsync(pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<OrderDto> orderDtos = items.Adapt<IReadOnlyList<OrderDto>>();
        return (orderDtos, totalCount);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        // Validate customer exists
        Customer? customer = await _customerRepository.GetByIdAsync(createDto.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer == null)
        {
            _logger.LogWarning("Order creation failed: Customer not found. CustomerId: {CustomerId}", createDto.CustomerId);
            throw new ArgumentException($"Customer with ID {createDto.CustomerId} not found.", nameof(createDto));
        }

        // Validate shipping address exists
        Address? address = await _addressRepository.GetByIdAsync(createDto.ShippingAddressId, cancellationToken).ConfigureAwait(false);
        if (address == null)
        {
            _logger.LogWarning("Order creation failed: Shipping address not found. ShippingAddressId: {ShippingAddressId}", createDto.ShippingAddressId);
            throw new ArgumentException($"Shipping address with ID {createDto.ShippingAddressId} not found.", nameof(createDto));
        }

        // Validate address belongs to customer
        if (address.CustomerId != createDto.CustomerId)
        {
            _logger.LogWarning("Order creation failed: Shipping address does not belong to customer. ShippingAddressId: {ShippingAddressId}, CustomerId: {CustomerId}, AddressCustomerId: {AddressCustomerId}",
                createDto.ShippingAddressId, createDto.CustomerId, address.CustomerId);
            throw new ArgumentException($"Shipping address {createDto.ShippingAddressId} does not belong to customer {createDto.CustomerId}.", nameof(createDto));
        }

        _logger.LogInformation("Creating new order. CustomerId: {CustomerId}, ShippingAddressId: {ShippingAddressId}", createDto.CustomerId, createDto.ShippingAddressId);
        Order order = createDto.Adapt<Order>();
        order.OrderTimestamp = DateTime.UtcNow;
        await _orderRepository.AddAsync(order, cancellationToken).ConfigureAwait(false);
        await _orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Order created successfully. OrderId: {OrderId}", order.Id);
        return order.Adapt<OrderDto>();
    }

    public async Task<IReadOnlyList<OrderDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        List<Order> orders = await _orderRepository.GetByCustomerIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        return orders.Adapt<IReadOnlyList<OrderDto>>();
    }

    public async Task<IReadOnlyList<OrderDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        List<Order> orders = await _orderRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);
        return orders.Adapt<IReadOnlyList<OrderDto>>();
    }

    public async Task<OrderDto?> UpdateAsync(int id, UpdateOrderDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        _logger.LogInformation("Updating order. OrderId: {OrderId}", id);
        Order? order = await _orderRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (order == null)
        {
            return null;
        }

        updateDto.Adapt(order);
        await _orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await _orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Order updated successfully. OrderId: {OrderId}", id);
        return order.Adapt<OrderDto>();
    }

    public async Task<OrderDto?> PatchAsync(int id, UpdateOrderDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto, nameof(patchDto));

        Order? order = await _orderRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (order == null)
        {
            return null;
        }

        bool hasChanges = false;

        if (patchDto.CustomerId.HasValue && order.CustomerId != patchDto.CustomerId.Value)
        {
            order.CustomerId = patchDto.CustomerId.Value;
            hasChanges = true;
        }

        if (patchDto.OrderTimestamp.HasValue && order.OrderTimestamp != patchDto.OrderTimestamp.Value)
        {
            order.OrderTimestamp = patchDto.OrderTimestamp.Value;
            hasChanges = true;
        }

        if (patchDto.ShippingAddressId.HasValue && order.ShippingAddressId != patchDto.ShippingAddressId.Value)
        {
            order.ShippingAddressId = patchDto.ShippingAddressId.Value;
            hasChanges = true;
        }

        if (patchDto.Total.HasValue && order.Total != patchDto.Total.Value)
        {
            order.Total = patchDto.Total.Value;
            hasChanges = true;
        }

        if (patchDto.ShippingCost.HasValue && order.ShippingCost != patchDto.ShippingCost.Value)
        {
            order.ShippingCost = patchDto.ShippingCost.Value;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
            await _orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Order patched successfully. OrderId: {OrderId}", id);
        }

        return order.Adapt<OrderDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting order. OrderId: {OrderId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await _orderRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        // Check if already soft-deleted
        Order? order = await _orderRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (order == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            _logger.LogInformation("Order already deleted. OrderId: {OrderId}", id);
            return true;
        }

        // Perform soft delete
        await _orderRepository.DeleteAsync(order, cancellationToken).ConfigureAwait(false);
        await _orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Order deleted successfully. OrderId: {OrderId}", id);
        return true;
    }

    public async Task<IReadOnlyList<OrderDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateOrderDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates, nameof(updates));

        if (updates.Count == 0)
        {
            return Array.Empty<OrderDto>();
        }

        _logger.LogInformation("Updating {Count} orders in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Order> orders = await _orderRepository.FindAsync(o => ids.Contains(o.Id), cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Order> orderLookup = orders.ToDictionary(o => o.Id);

        List<OrderDto> updatedOrders = new(updates.Count);
        foreach ((int id, UpdateOrderDto updateDto) in updates)
        {
            if (orderLookup.TryGetValue(id, out Order? order))
            {
                updateDto.Adapt(order);
                await _orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
                updatedOrders.Add(order.Adapt<OrderDto>());
            }
        }

        await _orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch updated {Count} orders successfully", updatedOrders.Count);
        return updatedOrders;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        _logger.LogInformation("Deleting {Count} orders in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Order> orders = await _orderRepository.FindAsync(o => ids.Contains(o.Id), cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(orders.Count);
        foreach (Order order in orders)
        {
            await _orderRepository.DeleteAsync(order, cancellationToken).ConfigureAwait(false);
            deletedIds.Add(order.Id);
        }

        await _orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch deleted {Count} orders successfully", deletedIds.Count);
        return deletedIds;
    }
}

