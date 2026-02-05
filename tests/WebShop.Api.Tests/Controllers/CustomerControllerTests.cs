using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Controllers;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;
using Xunit;

namespace WebShop.Api.Tests.Controllers;

/// <summary>
/// Unit tests for CustomerController.
/// </summary>
[Trait("Category", "Unit")]
public class CustomerControllerTests
{
    private readonly Mock<ICustomerService> _mockService;
    private readonly Mock<ILogger<CustomerController>> _mockLogger;
    private readonly CustomerController _controller;

    public CustomerControllerTests()
    {
        _mockService = new Mock<ICustomerService>();
        _mockLogger = new Mock<ILogger<CustomerController>>();
        _controller = new CustomerController(_mockService.Object, _mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithCustomers()
    {
        // Arrange
        List<CustomerDto> customers = new List<CustomerDto>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
        };

        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act (non-paginated - PaginationQuery with Page=0)
        IActionResult result = await _controller.GetAll(new PaginationQuery(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<Response<IReadOnlyList<CustomerDto>>>();
        Response<IReadOnlyList<CustomerDto>>? response = okResult.Value as Response<IReadOnlyList<CustomerDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
        _mockService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CustomerDto>());

        // Act (non-paginated - PaginationQuery with Page=0)
        IActionResult result = await _controller.GetAll(new PaginationQuery(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result as OkObjectResult;
        Response<IReadOnlyList<CustomerDto>>? response = okResult!.Value as Response<IReadOnlyList<CustomerDto>>;
        response!.Data.Should().BeEmpty();
        response.Succeeded.Should().BeTrue();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsOkWithCustomer()
    {
        // Arrange
        const int customerId = 1;
        CustomerDto customer = new CustomerDto
        {
            Id = customerId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        _mockService
            .Setup(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        ActionResult<Response<CustomerDto>> result = await _controller.GetById(customerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<CustomerDto>? response = okResult!.Value as Response<CustomerDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(customerId);
        response.Succeeded.Should().BeTrue();
        _mockService.Verify(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int customerId = 999;
        _mockService
            .Setup(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDto?)null);

        // Act
        ActionResult<Response<CustomerDto>> result = await _controller.GetById(customerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<CustomerDto>? response = notFoundResult!.Value as Response<CustomerDto>;
        response!.Succeeded.Should().BeFalse();
        _mockService.Verify(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        CreateCustomerDto createDto = new CreateCustomerDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        CustomerDto createdCustomer = new CustomerDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        _mockService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCustomer);

        // Act
        ActionResult<Response<CustomerDto>> result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        CreatedAtActionResult? createdResult = result.Result as CreatedAtActionResult;
        Response<CustomerDto>? response = createdResult!.Value as Response<CustomerDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(1);
        response.Succeeded.Should().BeTrue();
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int customerId = 1;
        UpdateCustomerDto updateDto = new UpdateCustomerDto
        {
            FirstName = "John Updated",
            LastName = "Doe Updated",
            Email = "john.updated@example.com"
        };

        CustomerDto updatedCustomer = new CustomerDto
        {
            Id = customerId,
            FirstName = "John Updated",
            LastName = "Doe Updated",
            Email = "john.updated@example.com"
        };

        _mockService
            .Setup(s => s.UpdateAsync(customerId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCustomer);

        // Act
        IActionResult result = await _controller.Update(customerId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockService.Verify(s => s.UpdateAsync(customerId, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int customerId = 999;
        UpdateCustomerDto updateDto = new UpdateCustomerDto
        {
            FirstName = "John Updated",
            LastName = "Doe Updated",
            Email = "john.updated@example.com"
        };

        _mockService
            .Setup(s => s.UpdateAsync(customerId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDto?)null);

        // Act
        IActionResult result = await _controller.Update(customerId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        _mockService.Verify(s => s.UpdateAsync(customerId, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int customerId = 1;
        _mockService
            .Setup(s => s.DeleteAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        IActionResult result = await _controller.Delete(customerId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockService.Verify(s => s.DeleteAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int customerId = 999;
        _mockService
            .Setup(s => s.DeleteAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        IActionResult result = await _controller.Delete(customerId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        _mockService.Verify(s => s.DeleteAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetByEmail Tests

    [Fact]
    public async Task GetByEmail_ValidEmail_ReturnsOkWithCustomer()
    {
        // Arrange
        const string email = "john.doe@example.com";
        CustomerDto customer = new CustomerDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = email
        };

        _mockService
            .Setup(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        ActionResult<Response<CustomerDto>> result = await _controller.GetByEmail(email, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<CustomerDto>? response = okResult!.Value as Response<CustomerDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.Email.Should().Be(email);
        response.Succeeded.Should().BeTrue();
        _mockService.Verify(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByEmail_InvalidEmail_ReturnsNotFound()
    {
        // Arrange
        const string email = "nonexistent@example.com";
        _mockService
            .Setup(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDto?)null);

        // Act
        ActionResult<Response<CustomerDto>> result = await _controller.GetByEmail(email, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<CustomerDto>? response = notFoundResult!.Value as Response<CustomerDto>;
        response!.Succeeded.Should().BeFalse();
        _mockService.Verify(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Patch Tests

    [Fact]
    public async Task Patch_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int customerId = 1;
        UpdateCustomerDto patchDto = new UpdateCustomerDto
        {
            FirstName = "John Updated",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        CustomerDto updatedCustomer = new CustomerDto
        {
            Id = customerId,
            FirstName = "John Updated",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        _mockService
            .Setup(s => s.UpdateAsync(customerId, patchDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCustomer);

        // Act
        IActionResult result = await _controller.Patch(customerId, patchDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Patch_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int customerId = 999;
        UpdateCustomerDto patchDto = new UpdateCustomerDto
        {
            FirstName = "John Updated"
        };

        _mockService
            .Setup(s => s.UpdateAsync(customerId, patchDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDto?)null);

        // Act
        IActionResult result = await _controller.Patch(customerId, patchDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateBatch Tests

    [Fact]
    public async Task CreateBatch_ValidDtos_ReturnsCreated()
    {
        // Arrange
        List<CreateCustomerDto> createDtos = new List<CreateCustomerDto>
        {
            new() { FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            new() { FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
        };

        List<CustomerDto> createdCustomers = new List<CustomerDto>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
        };

        _mockService
            .Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCustomers);

        // Act
        ActionResult<Response<IReadOnlyList<CustomerDto>>> result = await _controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
        Response<IReadOnlyList<CustomerDto>>? response = objectResult.Value as Response<IReadOnlyList<CustomerDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
        _mockService.Verify(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateBatch Tests

    [Fact]
    public async Task UpdateBatch_ValidUpdates_ReturnsOk()
    {
        // Arrange
        List<BatchUpdateRequest<UpdateCustomerDto>> updates = new List<BatchUpdateRequest<UpdateCustomerDto>>
        {
            new() { Id = 1, Data = new UpdateCustomerDto { FirstName = "John Updated" } },
            new() { Id = 2, Data = new UpdateCustomerDto { FirstName = "Jane Updated" } }
        };

        List<CustomerDto> updatedCustomers = new List<CustomerDto>
        {
            new() { Id = 1, FirstName = "John Updated", LastName = "Doe", Email = "john@example.com" },
            new() { Id = 2, FirstName = "Jane Updated", LastName = "Smith", Email = "jane@example.com" }
        };

        _mockService
            .Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateCustomerDto UpdateDto)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCustomers);

        // Act
        ActionResult<Response<IReadOnlyList<CustomerDto>>> result = await _controller.UpdateBatch(updates, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<CustomerDto>>? response = okResult!.Value as Response<IReadOnlyList<CustomerDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
        _mockService.Verify(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateCustomerDto UpdateDto)>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteBatch Tests

    [Fact]
    public async Task DeleteBatch_ValidIds_ReturnsOk()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2 };
        List<int> deletedIds = new List<int> { 1, 2 };

        _mockService
            .Setup(s => s.DeleteBatchAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedIds);

        // Act
        ActionResult<Response<IReadOnlyList<int>>> result = await _controller.DeleteBatch(ids, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<int>>? response = okResult!.Value as Response<IReadOnlyList<int>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
        _mockService.Verify(s => s.DeleteBatchAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public async Task GetAll_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert (non-paginated - PaginationQuery with Page=0)
        Func<Task> act = async () => await _controller.GetAll(new PaginationQuery(), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetById_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        const int customerId = 1;
        _mockService
            .Setup(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _controller.GetById(customerId, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Create_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        CreateCustomerDto createDto = new CreateCustomerDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };
        _mockService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _controller.Create(createDto, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Update_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        const int customerId = 1;
        UpdateCustomerDto updateDto = new UpdateCustomerDto { FirstName = "Updated John" };
        _mockService
            .Setup(s => s.UpdateAsync(customerId, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _controller.Update(customerId, updateDto, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Patch_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        const int customerId = 1;

        UpdateCustomerDto patchDto = new UpdateCustomerDto
        {
            FirstName = "Patched John"
        };

        _mockService
            .Setup(s => s.UpdateAsync(customerId, patchDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _controller.Patch(customerId, patchDto, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Delete_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        const int customerId = 1;
        _mockService
            .Setup(s => s.DeleteAsync(customerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _controller.Delete(customerId, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #region Additional Error Handling Tests

    [Fact]
    public async Task GetById_NegativeId_ReturnsNotFound()
    {
        // Arrange
        const int customerId = -1;

        // Act
        ActionResult<Response<CustomerDto>> result = await _controller.GetById(customerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        CreateCustomerDto createDto = new CreateCustomerDto(); // Empty DTO
        _controller.ModelState.AddModelError("FirstName", "FirstName is required");

        // Act
        ActionResult<Response<CustomerDto>> result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        BadRequestObjectResult? badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
    }


    [Fact]
    public async Task CreateBatch_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        List<CreateCustomerDto> createDtos = new List<CreateCustomerDto>();
        _mockService
            .Setup(s => s.CreateBatchAsync(It.IsAny<IReadOnlyList<CreateCustomerDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerDto>());

        // Act - CreateBatch returns 201 Created
        ActionResult<Response<IReadOnlyList<CustomerDto>>> result = await _controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status201Created);
        Response<IReadOnlyList<CustomerDto>>? response = objectResult.Value as Response<IReadOnlyList<CustomerDto>>;
        response.Should().NotBeNull();
        response!.Data.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task UpdateBatch_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        List<BatchUpdateRequest<UpdateCustomerDto>> updates = new List<BatchUpdateRequest<UpdateCustomerDto>>();
        _mockService
            .Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateCustomerDto UpdateDto)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerDto>());

        // Act
        ActionResult<Response<IReadOnlyList<CustomerDto>>> result = await _controller.UpdateBatch(updates, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        Response<IReadOnlyList<CustomerDto>>? response = okResult.Value as Response<IReadOnlyList<CustomerDto>>;
        response.Should().NotBeNull();
        response!.Data.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task DeleteBatch_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        List<int> ids = new List<int>();
        _mockService
            .Setup(s => s.DeleteBatchAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>());

        // Act
        ActionResult<Response<IReadOnlyList<int>>> result = await _controller.DeleteBatch(ids, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        Response<IReadOnlyList<int>>? response = okResult.Value as Response<IReadOnlyList<int>>;
        response.Should().NotBeNull();
        response!.Data.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #endregion
}
