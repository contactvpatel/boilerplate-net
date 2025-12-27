using WebShop.Api.Extensions.Utilities;

namespace WebShop.Api.Filters;

/// <summary>
/// Startup filter that validates database connections before the application starts serving requests.
/// Implements fail-fast pattern to detect connection issues early.
/// </summary>
public class DatabaseConnectionValidationFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.ValidateDatabaseConnections();
            next(app);
        };
    }
}

