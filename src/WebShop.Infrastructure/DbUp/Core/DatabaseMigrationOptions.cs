namespace WebShop.Infrastructure.DbUp.Core;

public class DatabaseMigrationOptions
{
    public const string SectionName = "DatabaseMigration";

    public bool Enabled { get; set; } = false;

    public long PostgresqlAdvisoryLockKey { get; set; } = 123456789;
}