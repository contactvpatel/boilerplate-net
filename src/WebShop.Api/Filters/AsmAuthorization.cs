using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace WebShop.Api.Filters;

/// <summary>
/// Attribute for ASM (Application Security Management) authorization.
/// Supports multiple permissions with OR and AND logical conditions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AsmAuthorization : TypeFilterAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsmAuthorization"/> class with a single permission requirement.
    /// </summary>
    /// <param name="moduleCode">The module code to check access for.</param>
    /// <param name="accessType">The access type required.</param>
    /// <param name="logicalOperator">The logical operator to use when multiple attributes are present (default: OR).</param>
    public AsmAuthorization(ModuleCode moduleCode, AccessType accessType, LogicalOperator logicalOperator = LogicalOperator.OR)
        : base(typeof(AsmAuthorizationValidation))
    {
        PermissionRequirement permissionRequirement = new PermissionRequirement(moduleCode, accessType);
        Arguments = [new[] { permissionRequirement }, logicalOperator];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsmAuthorization"/> class with multiple permission requirements.
    /// </summary>
    /// <param name="permissions">Array of permission requirements to check.</param>
    /// <param name="logicalOperator">The logical operator to use between permissions (OR or AND).</param>
    public AsmAuthorization(PermissionRequirement[] permissions, LogicalOperator logicalOperator = LogicalOperator.OR)
        : base(typeof(AsmAuthorizationValidation))
    {
        Arguments = [permissions, logicalOperator];
    }
}

/// <summary>
/// Defines a permission requirement with module code and access type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PermissionRequirement"/> class.
/// </remarks>
/// <param name="moduleCode">The module code.</param>
/// <param name="accessType">The access type.</param>
public class PermissionRequirement(ModuleCode moduleCode, AccessType accessType)
{
    /// <summary>
    /// Gets the module code.
    /// </summary>
    public ModuleCode ModuleCode { get; } = moduleCode;

    /// <summary>
    /// Gets the access type.
    /// </summary>
    public AccessType AccessType { get; } = accessType;
}

/// <summary>
/// Module codes for the WebShop application.
/// </summary>
public enum ModuleCode
{
    [Description("CUST")] Customer,
    [Description("PROD")] Product,
    [Description("ORD")] Order,
    [Description("ADDR")] Address,
    [Description("ART")] Article,
    [Description("STOCK")] Stock,
    [Description("SIZE")] Size,
    [Description("COLOR")] Color,
    [Description("LABEL")] Label
}

/// <summary>
/// Access types for permissions.
/// </summary>
public enum AccessType
{
    View,
    Create,
    Update,
    Delete,
    Access,
    AllowAny
}

/// <summary>
/// Logical operators for combining multiple permission requirements.
/// </summary>
public enum LogicalOperator
{
    /// <summary>
    /// OR - User must have at least one of the specified permissions.
    /// </summary>
    OR,

    /// <summary>
    /// AND - User must have all of the specified permissions.
    /// </summary>
    AND
}
