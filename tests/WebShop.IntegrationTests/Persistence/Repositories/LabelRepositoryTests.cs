using FluentAssertions;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.IntegrationTests.Fixtures;
using Xunit;

namespace WebShop.IntegrationTests.Persistence.Repositories;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class LabelRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly LabelRepository _repository;

    public LabelRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        _repository = new LabelRepository(_fixture.ConnectionFactory, null, loggerFactory);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsLabel()
    {
        await _fixture.ResetDatabaseAsync();

        Label label = new() { Name = "Test Brand", SlugName = "test-brand", CreatedBy = 1, UpdatedBy = 1 };
        await _repository.AddAsync(label);

        Label? result = await _repository.GetByIdAsync(label.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(label.Id);
        result.Name.Should().Be("Test Brand");
        result.SlugName.Should().Be("test-brand");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        Label? result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsAllActiveLabels()
    {
        await _fixture.ResetDatabaseAsync();

        await _repository.AddAsync(new Label { Name = "Brand 1", SlugName = "brand-1", CreatedBy = 1, UpdatedBy = 1 });
        await _repository.AddAsync(new Label { Name = "Brand 2", SlugName = "brand-2", CreatedBy = 1, UpdatedBy = 1 });

        IReadOnlyList<Label> result = await _repository.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_ValidParams_ReturnsPagedResult()
    {
        await _fixture.ResetDatabaseAsync();

        for (int i = 0; i < 5; i++)
        {
            await _repository.AddAsync(new Label { Name = $"Brand{i}", SlugName = $"brand-{i}", CreatedBy = 1, UpdatedBy = 1 });
        }

        (IReadOnlyList<Label>? items, int totalCount) = await _repository.GetPagedAsync(1, 10);

        items.Should().NotBeNull();
        items.Should().HaveCount(5);
        totalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetBySlugNameAsync_ValidSlug_ReturnsLabel()
    {
        await _fixture.ResetDatabaseAsync();

        await _repository.AddAsync(new Label { Name = "Test Brand", SlugName = "test-brand", CreatedBy = 1, UpdatedBy = 1 });

        Label? result = await _repository.GetBySlugNameAsync("test-brand");

        result.Should().NotBeNull();
        result!.SlugName.Should().Be("test-brand");
    }

    [Fact]
    public async Task FindByIdsAsync_EmptyList_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();

        IReadOnlyList<Label> result = await _repository.FindByIdsAsync(Array.Empty<int>());

        result.Should().BeEmpty();
    }
}
