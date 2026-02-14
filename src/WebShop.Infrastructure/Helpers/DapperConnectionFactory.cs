using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
// using Microsoft.Data.SqlClient; // Uncomment for SQL Server support
using Npgsql;
using WebShop.Infrastructure.Interfaces;
using WebShop.Util;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Factory implementation for creating Dapper database connections with read/write separation.
/// </summary>
public class DapperConnectionFactory(
    IConfiguration configuration,
    ILogger<DapperConnectionFactory>? logger = null) : IDapperConnectionFactory
{
    private readonly (string Read, string Write) _connectionStrings = GetConnectionStrings(configuration);

    private static (string Read, string Write) GetConnectionStrings(IConfiguration configuration)
    {
        DbConnectionModel? databaseConnectionSettings = configuration.GetSection(ConfigurationKeys.DatabaseConnectionSettings)
            .Get<DbConnectionModel>() ?? configuration.GetSection(ConfigurationKeys.DbConnectionSettings)
            .Get<DbConnectionModel>() ?? throw new InvalidOperationException(
                "DatabaseConnectionSettings or DbConnectionSettings section not found in configuration. " +
                "Please ensure the configuration is properly set up.");

        string globalApplicationName = configuration.GetValue<string>(ConfigurationKeys.AppSettingsApplicationName) ?? "WebShop.Api";

        string read = DbConnectionStringCache.GetOrCreate(
            databaseConnectionSettings.Read,
            () => DbConnectionModel.CreateConnectionString(databaseConnectionSettings.Read, globalApplicationName));
        string write = DbConnectionStringCache.GetOrCreate(
            databaseConnectionSettings.Write,
            () => DbConnectionModel.CreateConnectionString(databaseConnectionSettings.Write, globalApplicationName));
        return (read, write);
    }

    public IDbConnection CreateReadConnection()
    {
        return CreateConnection(_connectionStrings.Read, "read");
    }

    public IDbConnection CreateWriteConnection()
    {
        return CreateConnection(_connectionStrings.Write, "write");
    }

    private IDbConnection CreateConnection(string connectionString, string connectionType)
    {
        IDbConnection connection = new NpgsqlConnection(connectionString);
        // If SQL Server: connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        logger?.LogDebug("Created {ConnectionType} connection for Dapper", connectionType);
        return connection;
    }
}
