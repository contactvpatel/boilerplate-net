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
/// Unit tests for ColorController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class ColorControllerTests
{
    private readonly Mock<IColorService> mockService = new();
    private readonly Mock<ILogger<ColorController>> mockLogger = new();
    private readonly ColorController controller;

    public ColorControllerTests()
    {
        controller = new ColorController(mockService.Object, mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithColors()
    {
        // Arrange
        List<ColorDto> colors =
        [
            new() { Id = 1, Name = "Red", Rgb = "#FF0000" },
            new() { Id = 2, Name = "Blue", Rgb = "#0000FF" }
        ];
        mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(colors);

        // Act
        ActionResult<Response<IReadOnlyList<ColorDto>>> result = await controller.GetAll(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<ColorDto>>? response = okResult!.Value as Response<IReadOnlyList<ColorDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsOkWithColor()
    {
        // Arrange
        const int colorId = 1;
        ColorDto color = new() { Id = colorId, Name = "Red", Rgb = "#FF0000" };
        mockService.Setup(s => s.GetByIdAsync(colorId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ColorDto>.Success(color));

        // Act
        ActionResult<Response<ColorDto>> result = await controller.GetById(colorId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<ColorDto>? response = okResult!.Value as Response<ColorDto>;
        response!.Data!.Id.Should().Be(colorId);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int colorId = 999;
        mockService.Setup(s => s.GetByIdAsync(colorId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ColorDto>.NotFound());

        // Act
        ActionResult<Response<ColorDto>> result = await controller.GetById(colorId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByName Tests

    [Fact]
    public async Task GetByName_ValidName_ReturnsOkWithColor()
    {
        // Arrange
        const string name = "Red";
        ColorDto color = new() { Id = 1, Name = name, Rgb = "#FF0000" };
        mockService.Setup(s => s.GetByNameAsync(name, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ColorDto>.Success(color));

        // Act
        ActionResult<Response<ColorDto>> result = await controller.GetByName(name, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<ColorDto>? response = okResult!.Value as Response<ColorDto>;
        response!.Data!.Name.Should().Be(name);
    }

    [Fact]
    public async Task GetByName_InvalidName_ReturnsNotFound()
    {
        // Arrange
        const string name = "NonExistent";
        mockService.Setup(s => s.GetByNameAsync(name, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ColorDto>.NotFound());

        // Act
        ActionResult<Response<ColorDto>> result = await controller.GetByName(name, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        CreateColorDto createDto = new() { Name = "Red", Rgb = "#FF0000" };
        ColorDto created = new() { Id = 1, Name = "Red", Rgb = "#FF0000" };
        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<ColorDto>> result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        CreatedAtActionResult? createdResult = result.Result as CreatedAtActionResult;
        Response<ColorDto>? response = createdResult!.Value as Response<ColorDto>;
        response!.Data!.Id.Should().Be(1);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int colorId = 1;
        UpdateColorDto updateDto = new() { Name = "Dark Red", Rgb = "#8B0000" };
        mockService.Setup(s => s.UpdateAsync(colorId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ColorDto>.Success(new ColorDto { Id = colorId }));

        // Act
        IActionResult result = await controller.Update(colorId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int colorId = 999;
        UpdateColorDto updateDto = new() { Name = "Dark Red" };
        mockService.Setup(s => s.UpdateAsync(colorId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ColorDto>.NotFound());

        // Act
        IActionResult result = await controller.Update(colorId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int colorId = 1;
        mockService.Setup(s => s.DeleteAsync(colorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        IActionResult result = await controller.Delete(colorId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int colorId = 999;
        mockService.Setup(s => s.DeleteAsync(colorId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        IActionResult result = await controller.Delete(colorId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateBatch Tests

    [Fact]
    public async Task CreateBatch_ValidDtos_ReturnsCreated()
    {
        // Arrange
        List<CreateColorDto> createDtos = [new() { Name = "Red", Rgb = "#FF0000" }];
        List<ColorDto> created = [new() { Id = 1, Name = "Red", Rgb = "#FF0000" }];
        mockService.Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<IReadOnlyList<ColorDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
    }

    #endregion
}
