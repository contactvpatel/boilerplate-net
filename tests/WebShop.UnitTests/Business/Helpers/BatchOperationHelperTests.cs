using FluentAssertions;
using Moq;
using WebShop.Business.Helpers;
using WebShop.Core.Interfaces.Base;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.Business.Helpers;

/// <summary>
/// Unit tests for BatchOperationHelper.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class BatchOperationHelperTests
{
    #region ExecuteInTransactionAsync Tests

    [Fact]
    public async Task ExecuteInTransactionAsync_OperationSucceeds_CommitsTransaction()
    {
        // Arrange
        Mock<IUnitOfWork> mockUnitOfWork = new();
        bool operationInvoked = false;
        CancellationToken? receivedToken = null;

        // Act
        await BatchOperationHelper.ExecuteInTransactionAsync(
            mockUnitOfWork.Object,
            ct =>
            {
                operationInvoked = true;
                receivedToken = ct;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        // Assert
        operationInvoked.Should().BeTrue();
        receivedToken.Should().Be(CancellationToken.None);
        mockUnitOfWork.Verify(u => u.BeginTransaction(), Times.Once);
        mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
        mockUnitOfWork.Verify(u => u.Rollback(), Times.Never);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_OperationThrows_RollsBackAndPropagatesException()
    {
        // Arrange
        Mock<IUnitOfWork> mockUnitOfWork = new();
        InvalidOperationException expectedException = new("Database error");

        // Act
        Func<Task> act = async () => await BatchOperationHelper.ExecuteInTransactionAsync(
            mockUnitOfWork.Object,
            _ => throw expectedException,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
        mockUnitOfWork.Verify(u => u.BeginTransaction(), Times.Once);
        mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
        mockUnitOfWork.Verify(u => u.Rollback(), Times.Once);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenCalled_OperationReceivesCancellationToken()
    {
        // Arrange
        Mock<IUnitOfWork> mockUnitOfWork = new();
        using CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;
        CancellationToken? receivedToken = null;

        // Act
        await BatchOperationHelper.ExecuteInTransactionAsync(
            mockUnitOfWork.Object,
            ct =>
            {
                receivedToken = ct;
                return Task.CompletedTask;
            },
            token);

        // Assert
        receivedToken.Should().Be(token);
    }

    #endregion

    #region ExecuteBatchAsync Tests

    [Fact]
    public async Task ExecuteBatchAsync_EmptyList_DoesNotInvokeOperationOrTransaction()
    {
        // Arrange
        Mock<IUnitOfWork> mockUnitOfWork = new();
        bool operationInvoked = false;

        // Act
        await BatchOperationHelper.ExecuteBatchAsync(
            mockUnitOfWork.Object,
            Array.Empty<int>(),
            (_, _) =>
            {
                operationInvoked = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        // Assert
        operationInvoked.Should().BeFalse();
        mockUnitOfWork.Verify(u => u.BeginTransaction(), Times.Never);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WithItems_InvokesOperationForEachItem()
    {
        // Arrange
        Mock<IUnitOfWork> mockUnitOfWork = new();
        List<int> processedItems = [];
        IReadOnlyList<int> items = [1, 2, 3];

        // Act
        await BatchOperationHelper.ExecuteBatchAsync(
            mockUnitOfWork.Object,
            items,
            (item, _) =>
            {
                processedItems.Add(item);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        // Assert
        processedItems.Should().Equal(1, 2, 3);
        mockUnitOfWork.Verify(u => u.BeginTransaction(), Times.Once);
        mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
    }

    [Fact]
    public async Task ExecuteBatchAsync_WhenCalled_OperationReceivesCancellationToken()
    {
        // Arrange
        Mock<IUnitOfWork> mockUnitOfWork = new();
        using CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;
        List<CancellationToken?> receivedTokens = [];

        // Act
        await BatchOperationHelper.ExecuteBatchAsync(
            mockUnitOfWork.Object,
            [1, 2],
            (_, ct) =>
            {
                receivedTokens.Add(ct);
                return Task.CompletedTask;
            },
            token);

        // Assert
        receivedTokens.Should().HaveCount(2);
        receivedTokens[0].Should().Be(token);
        receivedTokens[1].Should().Be(token);
    }

    [Fact]
    public async Task ExecuteBatchAsync_OperationThrows_RollsBackAndPropagatesException()
    {
        // Arrange
        Mock<IUnitOfWork> mockUnitOfWork = new();
        InvalidOperationException expectedException = new("Batch failed");

        // Act
        Func<Task> act = async () => await BatchOperationHelper.ExecuteBatchAsync(
            mockUnitOfWork.Object,
            [1, 2, 3],
            (item, _) => item == 2 ? throw expectedException : Task.CompletedTask,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Batch failed");
        mockUnitOfWork.Verify(u => u.Rollback(), Times.Once);
        mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    #endregion
}
