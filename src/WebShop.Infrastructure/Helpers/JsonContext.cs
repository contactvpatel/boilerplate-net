using System.Text.Json.Serialization;
using WebShop.Core.Models;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// JSON source generator context for optimized serialization/deserialization.
/// This provides compile-time code generation for better performance.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(SsoAuthResponse))]
[JsonSerializable(typeof(DepartmentModel))]
[JsonSerializable(typeof(IEnumerable<DepartmentModel>))]
[JsonSerializable(typeof(RoleTypeModel))]
[JsonSerializable(typeof(IEnumerable<RoleTypeModel>))]
[JsonSerializable(typeof(RoleModel))]
[JsonSerializable(typeof(IEnumerable<RoleModel>))]
[JsonSerializable(typeof(PositionModel))]
[JsonSerializable(typeof(IEnumerable<PositionModel>))]
[JsonSerializable(typeof(PersonPositionModel))]
[JsonSerializable(typeof(IEnumerable<PersonPositionModel>))]
[JsonSerializable(typeof(AsmResponseModel))]
[JsonSerializable(typeof(IEnumerable<AsmResponseModel>))]
[JsonSerializable(typeof(ValidateTokenRequest))]
[JsonSerializable(typeof(RenewTokenRequest))]
public partial class JsonContext : JsonSerializerContext
{
}

