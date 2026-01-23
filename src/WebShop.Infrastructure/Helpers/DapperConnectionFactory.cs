using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
// using Microsoft.Data.SqlClient; // Uncomment for SQL Server support
using Npgsql;
using WebShop.Infrastructure.Interfaces;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Factory implementation for creating Dapper database connections with read/write separation.
/// </summary>
public class DapperConnectionFactory : IDapperConnectionFactory
{
    private readonly string _readConnectionString;
    private readonly string _writeConnectionString;
    private readonly ILogger<DapperConnectionFactory>? _logger;

    public DapperConnectionFactory(
        IConfiguration configuration,
        ILogger<DapperConnectionFactory>? logger = null)
    {
        _logger = logger;

        // Get database connection settings from configuration
        // Try both "DatabaseConnectionSettings" and "DbConnectionSettings" for compatibility
        DbConnectionModel? databaseConnectionSettings = configuration.GetSection("DatabaseConnectionSettings")
            .Get<DbConnectionModel>() ?? configuration.GetSection("DbConnectionSettings")
            .Get<DbConnectionModel>() ?? throw new InvalidOperationException(
                "DatabaseConnectionSettings or DbConnectionSettings section not found in configuration. " +
                "Please ensure the configuration is properly set up.");

        // Get global application name from AppSettings to use as default for all connections
        string globalApplicationName = configuration.GetValue<string>("AppSettings:ApplicationName") ?? "WebShop.Api";

        // Cache connection strings to avoid recreating them on each connection creation
        _readConnectionString = DbConnectionStringCache.GetOrCreate(
            databaseConnectionSettings.Read,
            () => DbConnectionModel.CreateConnectionString(databaseConnectionSettings.Read, globalApplicationName));

        _writeConnectionString = DbConnectionStringCache.GetOrCreate(
            databaseConnectionSettings.Write,
            () => DbConnectionModel.CreateConnectionString(databaseConnectionSettings.Write, globalApplicationName));
    }

    public IDbConnection CreateReadConnection()
    {
        IDbConnection connection = new NpgsqlConnection(_readConnectionString);

        // If SQL Server support is enabled, uncomment the following line, add corresponding using directive and remove the above line for PostgreSQL

        // IDbConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_readConnectionString);

        _logger?.LogDebug("Created read connection for Dapper");
        return connection;
    }

    public IDbConnection CreateWriteConnection()
    {
        IDbConnection connection = new NpgsqlConnection(_writeConnectionString);

        // If SQL Server support is enabled, uncomment the following line, add corresponding using directive and remove the above line for PostgreSQL

        // IDbConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_writeConnectionString);

        _logger?.LogDebug("Created write connection for Dapper");

        return connection;
    }
}
