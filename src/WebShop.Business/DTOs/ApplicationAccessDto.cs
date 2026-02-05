namespace WebShop.Business.DTOs;

/// <summary>
/// One application or area and what the user can do in it: view, create, update, delete.
/// </summary>
public class ApplicationAccessDto
{
    /// <summary>Module code (e.g. SHR).</summary>
    public string ModuleCode { get; set; } = string.Empty;

    /// <summary>Display name of the module.</summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>Type of the module (e.g. Module).</summary>
    public string ModuleType { get; set; } = string.Empty;

    /// <summary>Identifier of the module.</summary>
    public int ModuleId { get; set; }

    /// <summary>Identifier of the module type.</summary>
    public int ModuleTypeId { get; set; }

    /// <summary>Whether the user can view this module.</summary>
    public bool? HasViewAccess { get; set; }

    /// <summary>Whether the user can create in this module.</summary>
    public bool? HasCreateAccess { get; set; }

    /// <summary>Whether the user can update in this module.</summary>
    public bool? HasUpdateAccess { get; set; }

    /// <summary>Whether the user can delete in this module.</summary>
    public bool? HasDeleteAccess { get; set; }

    /// <summary>Whether the user has general access to this module.</summary>
    public bool? HasAccess { get; set; }
}
