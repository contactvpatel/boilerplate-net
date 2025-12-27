using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Moq;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Helpers;
using WebShop.Infrastructure.Interfaces;

namespace WebShop.Infrastructure.Tests.Helpers;

/// <summary>
/// Helper class for creating mocked Dapper connections and repositories for unit testing.
/// </summary>
public static class DapperTestHelper
{
    /// <summary>
    /// Creates a mock IDbConnection that returns the specified data for QueryAsync calls.
    /// </summary>
    public static Mock<IDbConnection> CreateMockConnection<T>(IEnumerable<T> data) where T : class
    {
        Mock<IDbConnection> mockConnection = new Mock<IDbConnection>();
        Mock<IDbCommand> mockCommand = new Mock<IDbCommand>();
        Mock<IDataParameterCollection> mockParameterCollection = new Mock<IDataParameterCollection>();
        Mock<IDataReader> mockDataReader = new Mock<IDataReader>();

        // Setup command
        mockCommand.Setup(c => c.Parameters).Returns(mockParameterCollection.Object);
        mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);

        // Setup data reader to return the data
        List<T> dataList = data.ToList();
        int currentIndex = -1;

        mockDataReader.Setup(r => r.Read()).Returns(() =>
        {
            currentIndex++;
            return currentIndex < dataList.Count;
        });

        // Setup field accessors - this is simplified; real implementation would map properties
        mockDataReader.Setup(r => r.FieldCount).Returns(dataList.Count > 0 ? typeof(T).GetProperties().Length : 0);

        mockCommand.Setup(c => c.ExecuteReader()).Returns(mockDataReader.Object);

        return mockConnection;
    }

    /// <summary>
    /// Creates a mock IDapperConnectionFactory that returns the specified mock connections.
    /// </summary>
    public static Mock<IDapperConnectionFactory> CreateMockConnectionFactory(
        Mock<IDbConnection>? readConnection = null,
        Mock<IDbConnection>? writeConnection = null)
    {
        Mock<IDapperConnectionFactory> mockFactory = new Mock<IDapperConnectionFactory>();

        if (readConnection != null)
        {
            mockFactory.Setup(f => f.CreateReadConnection()).Returns(readConnection.Object);
        }

        if (writeConnection != null)
        {
            mockFactory.Setup(f => f.CreateWriteConnection()).Returns(writeConnection.Object);
        }

        return mockFactory;
    }

    /// <summary>
    /// Creates a mock IDapperTransactionManager.
    /// </summary>
    public static Mock<IDapperTransactionManager> CreateMockTransactionManager(
        Mock<IDbTransaction>? transaction = null)
    {
        Mock<IDapperTransactionManager> mockManager = new Mock<IDapperTransactionManager>();

        if (transaction != null)
        {
            mockManager.Setup(m => m.BeginTransaction()).Returns(transaction.Object);
            mockManager.Setup(m => m.GetCurrentTransaction()).Returns(transaction.Object);
        }

        return mockManager;
    }
}
