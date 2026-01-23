using System.ComponentModel;
using System.Reflection;

namespace WebShop.Api.Extensions.Utilities;

/// <summary>
/// Extension methods for enums.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the description attribute value from an enum value.
    /// </summary>
    /// <param name="value">The enum value.</param>
    /// <returns>The description if available, otherwise the enum name.</returns>
    public static string GetDescription(this Enum value)
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());
        DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
}
