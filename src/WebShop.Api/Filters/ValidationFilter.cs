using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebShop.Api.Models;

namespace WebShop.Api.Filters;

/// <summary>
/// Action filter that validates model state and returns standardized error responses.
/// This filter runs before controller actions and validates incoming request models.
/// </summary>
public class ValidationFilter(ILogger<ValidationFilter> logger) : IAsyncActionFilter
{
    private const string LogTemplate = "Area: {Area}, Action: {Action}, Controller: {Controller}, Version: {Version}, Message: {Message}";
    private readonly ILogger<ValidationFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Validates model state before executing the action.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    /// <param name="next">The action execution delegate.</param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context == null)
        {
            return;
        }

        if (!context.ModelState.IsValid)
        {
            string version = context.RouteData.Values["version"]?.ToString() ?? "Unknown";
            string controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
            string action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
            const string Area = "ValidationFilter.OnActionExecutionAsync";

            _logger.LogWarning(
                LogTemplate,
                Area,
                action,
                controller,
                version,
                "Model validation failed");

            List<ApiError> errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new ApiError
                {
                    ErrorId = Guid.NewGuid().ToString(),
                    StatusCode = (short)HttpStatusCode.BadRequest,
                    Message = $"{x.Key}: {e.ErrorMessage}"
                }))
                .ToList();

            Response<object?> response = new(null, false, "Validation failed", errors);
            context.Result = new BadRequestObjectResult(response);
            return;
        }

        await next();
    }
}

