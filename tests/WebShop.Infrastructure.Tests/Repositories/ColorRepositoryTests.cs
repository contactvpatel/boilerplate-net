using FluentAssertions;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.Infrastructure.Tests.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Repositories;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class ColorRepositoryTests
{
    private readonly Helpers.TestDatabaseFixture _fixture;
    private readonly ColorRepository _repository;

    public ColorRepositoryTests(Helpers.TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        _repository = new ColorRepository(_fixture.ConnectionFactory, null, loggerFactory);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsColor()
    {
        await _fixture.ResetDatabaseAsync();

        Color color = new() { Name = "Red", Rgb = "#FF0000", CreatedBy = 1, UpdatedBy = 1 };
        await _repository.AddAsync(color);

        Color? result = await _repository.GetByIdAsync(color.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(color.Id);
        result.Name.Should().Be("Red");
        result.Rgb.Should().Be("#FF0000");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        Color? result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveColors()
    {
        await _fixture.ResetDatabaseAsync();

        await _repository.AddAsync(new Color { Name = "Red", Rgb = "#FF0000", CreatedBy = 1, UpdatedBy = 1 });
        await _repository.AddAsync(new Color { Name = "Blue", Rgb = "#0000FF", CreatedBy = 1, UpdatedBy = 1 });

        IReadOnlyList<Color> result = await _repository.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_ValidParams_ReturnsPagedResult()
    {
        await _fixture.ResetDatabaseAsync();

        for (int i = 0; i < 6; i++)
        {
            await _repository.AddAsync(new Color { Name = $"Color{i}", Rgb = "#000000", CreatedBy = 1, UpdatedBy = 1 });
        }

        (IReadOnlyList<Color>? items, int totalCount) = await _repository.GetPagedAsync(1, 10);

        items.Should().NotBeNull();
        items.Should().HaveCount(6);
        totalCount.Should().Be(6);
    }

    [Fact]
    public async Task GetByNameAsync_ValidName_ReturnsColor()
    {
        await _fixture.ResetDatabaseAsync();

        await _repository.AddAsync(new Color { Name = "Red", Rgb = "#FF0000", CreatedBy = 1, UpdatedBy = 1 });

        Color? result = await _repository.GetByNameAsync("Red");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Red");
    }

    [Fact]
    public async Task FindByIdsAsync_EmptyList_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();

        IReadOnlyList<Color> result = await _repository.FindByIdsAsync(Array.Empty<int>());

        result.Should().BeEmpty();
    }
}
