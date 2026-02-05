namespace WebShop.Core.Models;

/// <summary>
/// One application (or area) and what the user can do in it: view, create, update, delete.
/// </summary>
public class ApplicationAccessModel
{
    /// <summary>Application or area code (e.g. SHR).</summary>
    public string ModuleCode { get; set; } = string.Empty;

    /// <summary>Display name of the application or area.</summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>Type of the application or area (e.g. Module).</summary>
    public string ModuleType { get; set; } = string.Empty;

    /// <summary>Identifier of the application or area.</summary>
    public int ModuleId { get; set; }

    /// <summary>Identifier of the application or area type.</summary>
    public int ModuleTypeId { get; set; }

    /// <summary>Whether the user can view in this application.</summary>
    public bool? HasViewAccess { get; set; }

    /// <summary>Whether the user can create in this application.</summary>
    public bool? HasCreateAccess { get; set; }

    /// <summary>Whether the user can update in this application.</summary>
    public bool? HasUpdateAccess { get; set; }

    /// <summary>Whether the user can delete in this application.</summary>
    public bool? HasDeleteAccess { get; set; }

    /// <summary>Whether the user has general access to this application or area.</summary>
    public bool? HasAccess { get; set; }
}
