namespace WebShop.Business.Helpers;

/// <summary>
/// Shared helper for applying partial (PATCH) updates.
/// Only updates entity fields when the patch DTO provides a non-null value that differs from the current value.
/// Reduces duplication across CustomerService, AddressService, and other services with PATCH operations.
/// </summary>
public static class PartialUpdateHelper
{
    /// <summary>
    /// Applies a string patch value if it is not null and differs from the current value.
    /// </summary>
    /// <param name="currentValue">The entity's current value.</param>
    /// <param name="patchValue">The patch DTO value (null means do not update).</param>
    /// <param name="setter">Action to set the new value on the entity.</param>
    /// <returns>True if the value was updated.</returns>
    public static bool ApplyIfChanged(string? currentValue, string? patchValue, Action<string?> setter)
    {
        if (patchValue == null)
        {
            return false;
        }

        if (string.Equals(currentValue, patchValue, StringComparison.Ordinal))
        {
            return false;
        }

        setter(patchValue);
        return true;
    }

    /// <summary>
    /// Applies a nullable value type patch if it has a value and differs from the current value.
    /// </summary>
    /// <param name="currentValue">The entity's current value.</param>
    /// <param name="patchValue">The patch DTO value (null means do not update).</param>
    /// <param name="setter">Action to set the new value on the entity.</param>
    /// <returns>True if the value was updated.</returns>
    public static bool ApplyIfChanged<T>(T? currentValue, T? patchValue, Action<T> setter) where T : struct
    {
        if (!patchValue.HasValue)
        {
            return false;
        }

        if (EqualityComparer<T>.Default.Equals(currentValue ?? default, patchValue.Value))
        {
            return false;
        }

        setter(patchValue.Value);
        return true;
    }
}
