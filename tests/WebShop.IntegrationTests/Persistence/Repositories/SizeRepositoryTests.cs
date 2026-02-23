using FluentAssertions;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.IntegrationTests.Fixtures;
using Xunit;

namespace WebShop.IntegrationTests.Persistence.Repositories;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class SizeRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly SizeRepository _repository;

    public SizeRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        _repository = new SizeRepository(_fixture.ConnectionFactory, null, loggerFactory);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsSize()
    {
        await _fixture.ResetDatabaseAsync();

        Size size = new() { Gender = "male", Category = "Apparel", SizeLabel = "M", SizeUs = "[32,34)", SizeUk = "[32,36)", SizeEu = "[42,48)", CreatedBy = 1, UpdatedBy = 1 };
        await _repository.AddAsync(size);

        Size? result = await _repository.GetByIdAsync(size.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(size.Id);
        result.Gender.Should().Be("male");
        result.Category.Should().Be("Apparel");
        result.SizeLabel.Should().Be("M");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        Size? result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsAllActiveSizes()
    {
        await _fixture.ResetDatabaseAsync();

        await _repository.AddAsync(new Size { Gender = "male", Category = "Apparel", SizeLabel = "M", SizeUs = "[32,34)", SizeUk = "[32,36)", SizeEu = "[42,48)", CreatedBy = 1, UpdatedBy = 1 });

        IReadOnlyList<Size> result = await _repository.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPagedAsync_ValidParams_ReturnsPagedResult()
    {
        await _fixture.ResetDatabaseAsync();

        for (int i = 0; i < 12; i++)
        {
            await _repository.AddAsync(new Size { Gender = "male", Category = "Apparel", SizeLabel = $"S{i}", SizeUs = "[32,34)", SizeUk = "[32,36)", SizeEu = "[42,48)", CreatedBy = 1, UpdatedBy = 1 });
        }

        (IReadOnlyList<Size>? items, int totalCount) = await _repository.GetPagedAsync(1, 10);

        items.Should().NotBeNull();
        items.Should().HaveCount(10);
        totalCount.Should().Be(12);
    }

    [Fact]
    public async Task GetByGenderAndCategoryAsync_ValidParams_ReturnsSizes()
    {
        await _fixture.ResetDatabaseAsync();

        await _repository.AddAsync(new Size { Gender = "male", Category = "Apparel", SizeLabel = "M", SizeUs = "[32,34)", SizeUk = "[32,36)", SizeEu = "[42,48)", CreatedBy = 1, UpdatedBy = 1 });

        List<Size> result = await _repository.GetByGenderAndCategoryAsync("male", "Apparel");

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Gender.Should().Be("male");
        result[0].Category.Should().Be("Apparel");
    }

    [Fact]
    public async Task FindByIdsAsync_EmptyList_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();

        IReadOnlyList<Size> result = await _repository.FindByIdsAsync(Array.Empty<int>());

        result.Should().BeEmpty();
    }
}
