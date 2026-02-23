using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Controllers;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Models;
using WebShop.Business.Services.Interfaces;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.API.Controllers;

/// <summary>
/// Unit tests for CustomerController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class CustomerControllerTests
{
    private readonly Mock<ICustomerService> mockService = new();
    private readonly Mock<ILogger<CustomerController>> mockLogger = new();
    private readonly CustomerController controller;

    public CustomerControllerTests()
    {
        controller = new CustomerController(mockService.Object, mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithCustomers()
    {
        // Arrange
        List<CustomerDto> customers = new List<CustomerDto>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
        };

        mockService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act (non-paginated - PaginationQuery with Page=0)
        IActionResult result = await controller.GetAll(new PaginationQuery(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<Response<IReadOnlyList<CustomerDto>>>();
        Response<IReadOnlyList<CustomerDto>>? response = okResult.Value as Response<IReadOnlyList<CustomerDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
        mockService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        mockService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CustomerDto>());

        // Act (non-paginated - PaginationQuery with Page=0)
        IActionResult result = await controller.GetAll(new PaginationQuery(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result as OkObjectResult;
        Response<IReadOnlyList<CustomerDto>>? response = okResult!.Value as Response<IReadOnlyList<CustomerDto>>;
        response!.Data.Should().BeEmpty();
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_Paginated_ReturnsPagedResult()
    {
        // Arrange
        List<CustomerDto> customers = new List<CustomerDto>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
        };
        const int totalCount = 50;

        mockService
            .Setup(s => s.GetPagedAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((customers, totalCount));

        PaginationQuery pagination = new() { Page = 1, PageSize = 20 };

        // Act
        IActionResult result = await controller.GetAll(pagination, CancellationToken.None);

        // Assert - OkResponse wraps PagedResult in Response<T>
        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<Response<PagedResult<CustomerDto>>>();
        Response<PagedResult<CustomerDto>>? response = okResult.Value as Response<PagedResult<CustomerDto>>;
        PagedResult<CustomerDto>? pagedResult = response!.Data;
        pagedResult!.Items.Should().HaveCount(2);
        pagedResult.TotalCount.Should().Be(50);
        pagedResult.PageNumber.Should().Be(1);
        pagedResult.PageSize.Should().Be(20);
        mockService.Verify(s => s.GetPagedAsync(1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_PaginatedEmptyPage_ReturnsEmptyPagedResult()
    {
        // Arrange
        mockService
            .Setup(s => s.GetPagedAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<CustomerDto>(), 0));

        PaginationQuery pagination = new() { Page = 1, PageSize = 20 };

        // Act
        IActionResult result = await controller.GetAll(pagination, CancellationToken.None);

        // Assert - OkResponse wraps PagedResult in Response<T>
        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result as OkObjectResult;
        Response<PagedResult<CustomerDto>>? response = okResult!.Value as Response<PagedResult<CustomerDto>>;
        PagedResult<CustomerDto>? pagedResult = response!.Data;
        pagedResult!.Items.Should().BeEmpty();
        pagedResult.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAll_InvalidPageSize_ReturnsFirstPage()
    {
        // Arrange - PageSize=0 is invalid; repository clamps to 1. IsPaginated requires Page>0, so we use Page=1.
        List<CustomerDto> customers = [new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" }];
        mockService
            .Setup(s => s.GetPagedAsync(1, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((customers, 50)); // Simulates clamped behavior returning first page

        PaginationQuery pagination = new() { Page = 1, PageSize = 0 }; // PageSize 0 triggers clamp in repository

        // Act
        IActionResult result = await controller.GetAll(pagination, CancellationToken.None);

        // Assert - Returns 200 with first page data
        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result as OkObjectResult;
        Response<PagedResult<CustomerDto>>? response = okResult!.Value as Response<PagedResult<CustomerDto>>;
        PagedResult<CustomerDto>? pagedResult = response!.Data;
        pagedResult!.Items.Should().HaveCount(1);
        pagedResult.TotalCount.Should().Be(50);
        mockService.Verify(s => s.GetPagedAsync(1, 0, It.IsAny<CancellationToken>()), Times.Once);
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

        mockService
            .Setup(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerDto>.Success(customer));

        // Act
        ActionResult<Response<CustomerDto>> result = await controller.GetById(customerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<CustomerDto>? response = okResult!.Value as Response<CustomerDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(customerId);
        response.Succeeded.Should().BeTrue();
        mockService.Verify(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int customerId = 999;
        mockService
            .Setup(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerDto>.NotFound());

        // Act
        ActionResult<Response<CustomerDto>> result = await controller.GetById(customerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<CustomerDto>? response = notFoundResult!.Value as Response<CustomerDto>;
        response!.Succeeded.Should().BeFalse();
        mockService.Verify(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
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

        mockService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerDto>.Success(createdCustomer));

        // Act
        ActionResult<Response<CustomerDto>> result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        CreatedAtActionResult? createdResult = result.Result as CreatedAtActionResult;
        Response<CustomerDto>? response = createdResult!.Value as Response<CustomerDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(1);
        response.Succeeded.Should().BeTrue();
        mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        CreateCustomerDto createDto = new CreateCustomerDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@example.com"
        };

        mockService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerDto>.Failure("Email address is already in use. Please use a different email address."));

        // Act
        ActionResult<Response<CustomerDto>> result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        BadRequestObjectResult? badRequestResult = result.Result as BadRequestObjectResult;
        Response<CustomerDto>? response = badRequestResult!.Value as Response<CustomerDto>;
        response!.Succeeded.Should().BeFalse();
        response.Errors.Should().NotBeNullOrEmpty();
        response.Errors![0].Message.Should().Contain("Email address is already in use");
        mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
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

        mockService
            .Setup(s => s.UpdateAsync(customerId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerDto>.Success(updatedCustomer));

        // Act
        IActionResult result = await controller.Update(customerId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        mockService.Verify(s => s.UpdateAsync(customerId, updateDto, It.IsAny<CancellationToken>()), Times.Once);
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

        mockService
            .Setup(s => s.UpdateAsync(customerId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerDto>.NotFound());

        // Act
        IActionResult result = await controller.Update(customerId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        mockService.Verify(s => s.UpdateAsync(customerId, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int customerId = 1;
        mockService
            .Setup(s => s.DeleteAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        IActionResult result = await controller.Delete(customerId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        mockService.Verify(s => s.DeleteAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int customerId = 999;
        mockService
            .Setup(s => s.DeleteAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        IActionResult result = await controller.Delete(customerId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        mockService.Verify(s => s.DeleteAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
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

        mockService
            .Setup(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerDto>.Success(customer));

        // Act
        ActionResult<Response<CustomerDto>> result = await controller.GetByEmail(email, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<CustomerDto>? response = okResult!.Value as Response<CustomerDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.Email.Should().Be(email);
        response.Succeeded.Should().BeTrue();
        mockService.Verify(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByEmail_InvalidEmail_ReturnsNotFound()
    {
        // Arrange
        const string email = "nonexistent@example.com";
        mockService
            .Setup(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerDto>.NotFound());

        // Act
        ActionResult<Response<CustomerDto>> result = await controller.GetByEmail(email, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<CustomerDto>? response = notFoundResult!.Value as Response<CustomerDto>;
        response!.Succeeded.Should().BeFalse();
        mockService.Verify(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
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

        mockService
            .Setup(s => s.PatchAsync(customerId, patchDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerDto>.Success(updatedCustomer));

        // Act
        IActionResult result = await controller.Patch(customerId, patchDto, CancellationToken.None);

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

        mockService
            .Setup(s => s.PatchAsync(customerId, patchDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerDto>.NotFound());

        // Act
        IActionResult result = await controller.Patch(customerId, patchDto, CancellationToken.None);

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

        mockService
            .Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCustomers);

        // Act
        ActionResult<Response<IReadOnlyList<CustomerDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
        Response<IReadOnlyList<CustomerDto>>? response = objectResult.Value as Response<IReadOnlyList<CustomerDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
        mockService.Verify(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>()), Times.Once);
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

        mockService
            .Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateCustomerDto UpdateDto)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCustomers);

        // Act
        ActionResult<Response<IReadOnlyList<CustomerDto>>> result = await controller.UpdateBatch(updates, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<CustomerDto>>? response = okResult!.Value as Response<IReadOnlyList<CustomerDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
        mockService.Verify(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateCustomerDto UpdateDto)>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteBatch Tests

    [Fact]
    public async Task DeleteBatch_ValidIds_ReturnsOk()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2 };
        List<int> deletedIds = new List<int> { 1, 2 };

        mockService
            .Setup(s => s.DeleteBatchAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedIds);

        // Act
        ActionResult<Response<IReadOnlyList<int>>> result = await controller.DeleteBatch(ids, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<int>>? response = okResult!.Value as Response<IReadOnlyList<int>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
        mockService.Verify(s => s.DeleteBatchAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public async Task GetAll_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        mockService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert (non-paginated - PaginationQuery with Page=0)
        Func<Task> act = async () => await controller.GetAll(new PaginationQuery(), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetById_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        const int customerId = 1;
        mockService
            .Setup(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await controller.GetById(customerId, CancellationToken.None);
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
        mockService
            .Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await controller.Create(createDto, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Update_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        const int customerId = 1;
        UpdateCustomerDto updateDto = new UpdateCustomerDto { FirstName = "Updated John" };
        mockService
            .Setup(s => s.UpdateAsync(customerId, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await controller.Update(customerId, updateDto, CancellationToken.None);
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

        mockService
            .Setup(s => s.PatchAsync(customerId, patchDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await controller.Patch(customerId, patchDto, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Delete_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        const int customerId = 1;
        mockService
            .Setup(s => s.DeleteAsync(customerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await controller.Delete(customerId, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #region Additional Error Handling Tests

    [Fact]
    public async Task GetById_NegativeId_ReturnsNotFound()
    {
        // Arrange
        const int customerId = -1;
        mockService
            .Setup(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CustomerDto>.NotFound());

        // Act
        ActionResult<Response<CustomerDto>> result = await controller.GetById(customerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);
        mockService.Verify(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBatch_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        List<CreateCustomerDto> createDtos = new List<CreateCustomerDto>();
        mockService
            .Setup(s => s.CreateBatchAsync(It.IsAny<IReadOnlyList<CreateCustomerDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerDto>());

        // Act - CreateBatch returns 201 Created
        ActionResult<Response<IReadOnlyList<CustomerDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

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
        mockService
            .Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateCustomerDto UpdateDto)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerDto>());

        // Act
        ActionResult<Response<IReadOnlyList<CustomerDto>>> result = await controller.UpdateBatch(updates, CancellationToken.None);

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
        mockService
            .Setup(s => s.DeleteBatchAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>());

        // Act
        ActionResult<Response<IReadOnlyList<int>>> result = await controller.DeleteBatch(ids, CancellationToken.None);

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
