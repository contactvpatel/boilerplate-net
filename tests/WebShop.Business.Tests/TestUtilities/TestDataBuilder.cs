using WebShop.Business.DTOs;
using WebShop.Core.Entities;

namespace WebShop.Business.Tests.TestUtilities;

/// <summary>
/// Test data builder for creating test entities and DTOs.
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a test customer entity with default values.
    /// </summary>
    public static Customer CreateCustomer(int id = 1, string? firstName = null, string? lastName = null, string? email = null)
    {
        return new Customer
        {
            Id = id,
            FirstName = firstName ?? "John",
            LastName = lastName ?? "Doe",
            Email = email ?? $"john.doe{id}@example.com",
            Gender = "Male",
            DateOfBirth = new DateTime(1990, 1, 1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1
        };
    }

    /// <summary>
    /// Creates a test customer DTO with default values.
    /// </summary>
    public static CustomerDto CreateCustomerDto(int id = 1, string? firstName = null, string? lastName = null, string? email = null)
    {
        return new CustomerDto
        {
            Id = id,
            FirstName = firstName ?? "John",
            LastName = lastName ?? "Doe",
            Email = email ?? $"john.doe{id}@example.com",
            Gender = "Male",
            DateOfBirth = new DateTime(1990, 1, 1),
            IsActive = true
        };
    }

    /// <summary>
    /// Creates a test CreateCustomerDto with default values.
    /// </summary>
    public static CreateCustomerDto CreateCreateCustomerDto(string? firstName = null, string? lastName = null, string? email = null)
    {
        return new CreateCustomerDto
        {
            FirstName = firstName ?? "John",
            LastName = lastName ?? "Doe",
            Email = email ?? "john.doe@example.com",
            Gender = "Male",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
    }

    /// <summary>
    /// Creates a test UpdateCustomerDto with default values.
    /// </summary>
    public static UpdateCustomerDto CreateUpdateCustomerDto(string? firstName = null, string? lastName = null, string? email = null)
    {
        return new UpdateCustomerDto
        {
            FirstName = firstName ?? "John Updated",
            LastName = lastName ?? "Doe Updated",
            Email = email ?? "john.updated@example.com",
            Gender = "Male",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
    }
}
