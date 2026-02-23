namespace WebShop.Util;

/// <summary>
/// Centralized configuration section and key names used across the solution.
/// Use these constants instead of magic strings to ensure consistency and enable refactoring.
/// </summary>
public static class ConfigurationKeys
{
    /// <summary>
    /// Root section for application settings (ApplicationName, Environment, EnableDatabaseMigration, etc.).
    /// </summary>
    public const string AppSettings = "AppSettings";

    /// <summary>
    /// Application name within AppSettings (e.g., "WebShop.Api").
    /// </summary>
    public const string AppSettingsApplicationName = "AppSettings:ApplicationName";

    /// <summary>
    /// Application version within AppSettings (e.g., "1.0.0").
    /// </summary>
    public const string AppSettingsApplicationVersion = "AppSettings:ApplicationVersion";

    /// <summary>
    /// Build number from environment variable (e.g., "123" or "1.2.3.45").
    /// Used in health endpoint and Scalar UI. Set via APP_BUILD_NUMBER env var.
    /// </summary>
    public const string AppBuildNumber = "APP_BUILD_NUMBER";

    /// <summary>
    /// Environment name within AppSettings (Dev, QA, UAT, Production).
    /// </summary>
    public const string AppSettingsEnvironment = "AppSettings:Environment";

    /// <summary>
    /// Enable ASM (Application Security Management) authorization (within AppSettings).
    /// </summary>
    public const string AppSettingsEnableAsmAuthorization = "AppSettings:EnableAsmAuthorization";

    /// <summary>
    /// Enable database migration on startup (within AppSettings).
    /// </summary>
    public const string AppSettingsEnableDatabaseMigration = "AppSettings:EnableDatabaseMigration";

    /// <summary>
    /// Database connection settings (Read/Write). Alias: DatabaseConnectionSettings.
    /// </summary>
    public const string DbConnectionSettings = "DbConnectionSettings";

    /// <summary>
    /// Database connection settings (Read/Write). Alias: DbConnectionSettings.
    /// </summary>
    public const string DatabaseConnectionSettings = "DatabaseConnectionSettings";

    /// <summary>
    /// HTTP resilience options (timeouts, retries, max request/response sizes).
    /// </summary>
    public const string HttpResilienceOptions = "HttpResilienceOptions";

    /// <summary>
    /// Cache options (expiration, stampede protection, etc.).
    /// </summary>
    public const string CacheOptions = "CacheOptions";

    /// <summary>
    /// Security headers configuration.
    /// </summary>
    public const string SecurityHeaders = "SecurityHeaders";

    /// <summary>
    /// API version deprecation options.
    /// </summary>
    public const string ApiVersionDeprecationOptions = "ApiVersionDeprecationOptions";

    /// <summary>
    /// CORS policy configuration.
    /// </summary>
    public const string CorsOptions = "CorsOptions";

    /// <summary>
    /// Rate limiting configuration.
    /// </summary>
    public const string RateLimitingOptions = "RateLimitingOptions";

    /// <summary>
    /// Response compression configuration.
    /// </summary>
    public const string ResponseCompressionOptions = "ResponseCompressionOptions";

    /// <summary>
    /// SSO service configuration.
    /// </summary>
    public const string SsoService = "SsoService";

    /// <summary>
    /// MIS (Management Information System) service configuration.
    /// </summary>
    public const string MisService = "MisService";

    /// <summary>
    /// ASM (Application Security Management) service configuration.
    /// </summary>
    public const string AsmService = "AsmService";
}
