using System.Reflection;
using DbUp.Engine;
using DbUp.Engine.Transactions;
using DbUp.ScriptProviders;

namespace WebShop.Infrastructure.DbUp.Core;

/// <summary>
/// Wraps an embedded script provider and strips the namespace prefix from script names
/// so that only the filename (e.g. "20250101-105000-ALM-001-Initial-Schema.sql") is stored in the SchemaVersions table.
/// </summary>
internal sealed class PrefixStrippingScriptProvider(
    Assembly assembly,
    string namespacePrefix,
    Func<string, bool> filter) : IScriptProvider
{
    private readonly EmbeddedScriptProvider _inner = new(assembly, filter);
    private readonly string _prefix = namespacePrefix.EndsWith('.') ? namespacePrefix : namespacePrefix + ".";

    public IEnumerable<SqlScript> GetScripts(IConnectionManager connectionManager)
    {
        return _inner.GetScripts(connectionManager)
            .Select(s => new SqlScript(
                s.Name.StartsWith(_prefix) ? s.Name[_prefix.Length..] : s.Name,
                s.Contents,
                s.SqlScriptOptions));
    }
}
