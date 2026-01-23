namespace WebShop.Util.Models;

/// <summary>
/// Application settings model for configuration
/// </summary>
public class AppSettingModel
{
    /// <summary>
    /// Application name
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Application version (e.g., "1.0.0")
    /// </summary>
    public string ApplicationVersion { get; set; } = string.Empty;

    /// <summary>
    /// Environment name (Dev, QA, UAT, Production)
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Enable ASM (Application Security Management) authorization
    /// </summary>
    public bool EnableAsmAuthorization { get; set; } = false;

    /// <summary>
    /// Enable database migration on startup
    /// </summary>
    public bool EnableDatabaseMigration { get; set; } = false;

    /// <summary>
    /// PostgreSQL advisory lock key for migration synchronization
    /// </summary>
    public long PostgresqlAdvisoryLockKey { get; set; } = 123456789;
}

