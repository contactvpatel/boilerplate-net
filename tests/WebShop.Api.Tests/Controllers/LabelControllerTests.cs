using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Controllers;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Models;
using WebShop.Business.Services.Interfaces;
using Xunit;

namespace WebShop.Api.Tests.Controllers;

/// <summary>
/// Unit tests for LabelController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class LabelControllerTests
{
    private readonly Mock<ILabelService> mockService = new();
    private readonly Mock<ILogger<LabelController>> mockLogger = new();
    private readonly LabelController controller;

    public LabelControllerTests()
    {
        controller = new LabelController(mockService.Object, mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithLabels()
    {
        // Arrange
        List<LabelDto> labels =
        [
            new() { Id = 1, Name = "Brand A", SlugName = "brand-a" },
            new() { Id = 2, Name = "Brand B", SlugName = "brand-b" }
        ];
        mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(labels);

        // Act
        ActionResult<Response<IReadOnlyList<LabelDto>>> result = await controller.GetAll(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<LabelDto>>? response = okResult!.Value as Response<IReadOnlyList<LabelDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsOkWithLabel()
    {
        // Arrange
        const int labelId = 1;
        LabelDto label = new() { Id = labelId, Name = "Brand A", SlugName = "brand-a" };
        mockService.Setup(s => s.GetByIdAsync(labelId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<LabelDto>.Success(label));

        // Act
        ActionResult<Response<LabelDto>> result = await controller.GetById(labelId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<LabelDto>? response = okResult!.Value as Response<LabelDto>;
        response!.Data!.Id.Should().Be(labelId);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int labelId = 999;
        mockService.Setup(s => s.GetByIdAsync(labelId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<LabelDto>.NotFound());

        // Act
        ActionResult<Response<LabelDto>> result = await controller.GetById(labelId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetBySlugName Tests

    [Fact]
    public async Task GetBySlugName_ValidSlug_ReturnsOkWithLabel()
    {
        // Arrange
        const string slugName = "brand-a";
        LabelDto label = new() { Id = 1, Name = "Brand A", SlugName = slugName };
        mockService.Setup(s => s.GetBySlugNameAsync(slugName, It.IsAny<CancellationToken>())).ReturnsAsync(Result<LabelDto>.Success(label));

        // Act
        ActionResult<Response<LabelDto>> result = await controller.GetBySlugName(slugName, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<LabelDto>? response = okResult!.Value as Response<LabelDto>;
        response!.Data!.SlugName.Should().Be(slugName);
    }

    [Fact]
    public async Task GetBySlugName_InvalidSlug_ReturnsNotFound()
    {
        // Arrange
        const string slugName = "non-existent";
        mockService.Setup(s => s.GetBySlugNameAsync(slugName, It.IsAny<CancellationToken>())).ReturnsAsync(Result<LabelDto>.NotFound());

        // Act
        ActionResult<Response<LabelDto>> result = await controller.GetBySlugName(slugName, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        CreateLabelDto createDto = new() { Name = "Brand A", SlugName = "brand-a" };
        LabelDto created = new() { Id = 1, Name = "Brand A", SlugName = "brand-a" };
        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<LabelDto>> result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        CreatedAtActionResult? createdResult = result.Result as CreatedAtActionResult;
        Response<LabelDto>? response = createdResult!.Value as Response<LabelDto>;
        response!.Data!.Id.Should().Be(1);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int labelId = 1;
        UpdateLabelDto updateDto = new() { Name = "Updated Brand", SlugName = "updated-brand" };
        mockService.Setup(s => s.UpdateAsync(labelId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<LabelDto>.Success(new LabelDto { Id = labelId }));

        // Act
        IActionResult result = await controller.Update(labelId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int labelId = 999;
        UpdateLabelDto updateDto = new() { Name = "Updated Brand" };
        mockService.Setup(s => s.UpdateAsync(labelId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<LabelDto>.NotFound());

        // Act
        IActionResult result = await controller.Update(labelId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int labelId = 1;
        mockService.Setup(s => s.DeleteAsync(labelId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        IActionResult result = await controller.Delete(labelId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int labelId = 999;
        mockService.Setup(s => s.DeleteAsync(labelId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        IActionResult result = await controller.Delete(labelId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateBatch Tests

    [Fact]
    public async Task CreateBatch_ValidDtos_ReturnsCreated()
    {
        // Arrange
        List<CreateLabelDto> createDtos = [new() { Name = "Brand A", SlugName = "brand-a" }];
        List<LabelDto> created = [new() { Id = 1, Name = "Brand A", SlugName = "brand-a" }];
        mockService.Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<IReadOnlyList<LabelDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
    }

    #endregion
}
