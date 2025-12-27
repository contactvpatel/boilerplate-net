using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Infrastructure.Helpers;
using WebShop.Infrastructure.Interfaces;

namespace WebShop.Infrastructure.Tests.Helpers;

/// <summary>
/// Helper class for creating mocked database connections for Dapper repository testing.
/// Uses Moq to mock IDbConnection and configure Dapper query results.
/// This approach provides fast, isolated unit tests without requiring a real database.
/// </summary>
public class DapperTestDatabase : IDisposable
{
    private readonly Mock<IDbConnection> _mockReadConnection;
    private readonly Mock<IDbConnection> _mockWriteConnection;
    private readonly Mock<IDapperConnectionFactory> _mockConnectionFactory;
    private readonly Mock<IDapperTransactionManager>? _mockTransactionManager;
    private bool _disposed;

    public DapperTestDatabase(ILoggerFactory? loggerFactory = null)
    {
        // Create mock connections
        _mockReadConnection = new Mock<IDbConnection>();
        _mockWriteConnection = new Mock<IDbConnection>();

        // Setup connection state
        _mockReadConnection.Setup(c => c.State).Returns(ConnectionState.Open);
        _mockWriteConnection.Setup(c => c.State).Returns(ConnectionState.Open);

        // Create mock connection factory
        _mockConnectionFactory = new Mock<IDapperConnectionFactory>();
        _mockConnectionFactory.Setup(f => f.CreateReadConnection()).Returns(_mockReadConnection.Object);
        _mockConnectionFactory.Setup(f => f.CreateWriteConnection()).Returns(_mockWriteConnection.Object);

        // Create transaction manager mock (optional)
        _mockTransactionManager = new Mock<IDapperTransactionManager>();
    }

    public IDapperConnectionFactory ConnectionFactory => _mockConnectionFactory.Object;
    public IDapperTransactionManager? TransactionManager => _mockTransactionManager?.Object;

    /// <summary>
    /// Sets up a mock query result for Dapper QueryAsync calls.
    /// Accepts dictionaries that represent dynamic rows (as Dapper returns dynamic objects).
    /// </summary>
    public void SetupQuery(IEnumerable<Dictionary<string, object>> results)
    {
        SetupQueryInternal(_mockReadConnection, results);
    }

    /// <summary>
    /// Sets up a mock query result for write connection.
    /// </summary>
    public void SetupWriteQuery(IEnumerable<Dictionary<string, object>> results)
    {
        SetupQueryInternal(_mockWriteConnection, results);
    }

    private void SetupQueryInternal(Mock<IDbConnection> mockConnection, IEnumerable<Dictionary<string, object>> results)
    {
        List<Dictionary<string, object>> resultsList = results.ToList();
        Mock<IDbCommand> mockCommand = new Mock<IDbCommand>();
        Mock<IDataParameterCollection> mockParameterCollection = new Mock<IDataParameterCollection>();
        Mock<IDataReader> mockDataReader = new Mock<IDataReader>();

        mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
        mockCommand.Setup(c => c.Parameters).Returns(mockParameterCollection.Object);
        mockCommand.Setup(c => c.Connection).Returns(mockConnection.Object);

        // Setup data reader to return the results
        int currentIndex = -1;

        mockDataReader.Setup(r => r.Read()).Returns(() =>
        {
            currentIndex++;
            return currentIndex < resultsList.Count;
        });

        if (resultsList.Count > 0)
        {
            // Get all unique keys from all dictionaries
            List<string> allKeys = resultsList.SelectMany(d => d.Keys).Distinct().ToList();
            mockDataReader.Setup(r => r.FieldCount).Returns(allKeys.Count);

            // Setup indexer accessors for each key (both original and lowercase)
            foreach (string key in allKeys)
            {
                string keyCopy = key; // Capture for closure

                // Setup indexer by original key name
                mockDataReader.Setup(r => r[keyCopy]).Returns(() =>
                    currentIndex >= 0 && currentIndex < resultsList.Count && resultsList[currentIndex].TryGetValue(keyCopy, out object? value)
                        ? value ?? DBNull.Value
                        : DBNull.Value);

                // Setup indexer by lowercase name (for database column names)
                string lowerKey = keyCopy.ToLowerInvariant();
                if (lowerKey != keyCopy)
                {
                    mockDataReader.Setup(r => r[lowerKey]).Returns(() =>
                        currentIndex >= 0 && currentIndex < resultsList.Count && resultsList[currentIndex].TryGetValue(keyCopy, out object? value)
                            ? value ?? DBNull.Value
                            : DBNull.Value);
                }
            }
        }
        else
        {
            mockDataReader.Setup(r => r.FieldCount).Returns(0);
        }

        mockCommand.Setup(c => c.ExecuteReader()).Returns(mockDataReader.Object);
        mockCommand.Setup(c => c.ExecuteReader(It.IsAny<CommandBehavior>())).Returns(mockDataReader.Object);
    }

    /// <summary>
    /// Sets up a mock query result for Dapper QueryFirstOrDefaultAsync calls.
    /// </summary>
    public void SetupQueryFirstOrDefault(Dictionary<string, object>? result)
    {
        if (result == null)
        {
            SetupQuery(Array.Empty<Dictionary<string, object>>());
        }
        else
        {
            SetupQuery(new[] { result });
        }
    }

    /// <summary>
    /// Sets up a mock execute result (for INSERT, UPDATE, DELETE).
    /// </summary>
    public void SetupExecute(int rowsAffected)
    {
        SetupExecuteInternal(_mockWriteConnection, rowsAffected);
    }

    /// <summary>
    /// Sets up a mock execute result for read connection (for EXISTS queries).
    /// </summary>
    public void SetupReadExecute(int rowsAffected)
    {
        SetupExecuteInternal(_mockReadConnection, rowsAffected);
    }

    private void SetupExecuteInternal(Mock<IDbConnection> mockConnection, int rowsAffected)
    {
        Mock<IDbCommand> mockCommand = new Mock<IDbCommand>();
        Mock<IDataParameterCollection> mockParameterCollection = new Mock<IDataParameterCollection>();
        Mock<IDataReader> mockDataReader = new Mock<IDataReader>();

        mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
        mockCommand.Setup(c => c.Parameters).Returns(mockParameterCollection.Object);
        mockCommand.Setup(c => c.Connection).Returns(mockConnection.Object);
        mockCommand.Setup(c => c.ExecuteNonQuery()).Returns(rowsAffected);

        // For EXISTS queries, setup a data reader that returns a boolean
        mockDataReader.Setup(r => r.Read()).Returns(true);
        mockDataReader.Setup(r => r.FieldCount).Returns(1);
        mockDataReader.Setup(r => r[0]).Returns(rowsAffected > 0 ? (object)1 : (object)0);
        mockDataReader.Setup(r => r.GetBoolean(0)).Returns(rowsAffected > 0);
        mockDataReader.Setup(r => r.GetInt32(0)).Returns(rowsAffected > 0 ? 1 : 0);

        mockCommand.Setup(c => c.ExecuteReader()).Returns(mockDataReader.Object);
        mockCommand.Setup(c => c.ExecuteReader(It.IsAny<CommandBehavior>())).Returns(mockDataReader.Object);
    }

    /// <summary>
    /// Sets up a mock scalar result (for EXISTS queries).
    /// Dapper's QueryFirstOrDefaultAsync<bool> for EXISTS queries will use QueryAsync internally.
    /// </summary>
    public void SetupScalar(bool result)
    {
        // For EXISTS queries, Dapper uses QueryFirstOrDefaultAsync<bool>
        // The query returns a single boolean value, so we create a dictionary with a boolean
        // Dapper will map this to a bool
        Dictionary<string, object> resultDict = new Dictionary<string, object>
        {
            { "exists", result }
        };
        SetupQueryFirstOrDefault(resultDict);
    }

    /// <summary>
    /// Sets up a mock scalar result for integer values (for QuerySingleAsync<int>).
    /// </summary>
    public void SetupScalar(int result)
    {
        Mock<IDbCommand> mockCommand = new Mock<IDbCommand>();
        Mock<IDataParameterCollection> mockParameterCollection = new Mock<IDataParameterCollection>();

        _mockWriteConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
        mockCommand.Setup(c => c.Parameters).Returns(mockParameterCollection.Object);
        mockCommand.Setup(c => c.Connection).Returns(_mockWriteConnection.Object);
        mockCommand.Setup(c => c.ExecuteScalar()).Returns(result);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _mockReadConnection?.Object?.Dispose();
        _mockWriteConnection?.Object?.Dispose();
        _disposed = true;
    }
}
