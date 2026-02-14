using Microsoft.AspNetCore.Http;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Infrastructure.Services.Internal;

/// <summary>
/// Infrastructure implementation of user context that extracts user information from HTTP context.
/// </summary>
public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private const string UserIdKey = "UserId";
    private const string UserTokenKey = "UserToken";


    /// <inheritdoc />
    public string? GetUserId()
    {
        return httpContextAccessor.HttpContext?.Items[UserIdKey] as string;
    }

    /// <inheritdoc />
    public string? GetToken()
    {
        return httpContextAccessor.HttpContext?.Items[UserTokenKey] as string;
    }
}
