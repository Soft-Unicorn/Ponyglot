using System;
using System.Reflection;

namespace Ponyglot.Sources;

/// <summary>
/// Defines the options for the <see cref="AssemblyCatalogSource"/>.
/// </summary>
public class AssemblyCatalogSourceOptions
{
    /// <summary>
    /// The optional filter that can be used to exclude specific embedded resources. Default is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// The filter receives the <see cref="System.Reflection.Assembly"/> and resource name.
    /// The filter has to be fast and deterministic. It will be called once for each discovered embedded resource.
    /// </remarks>
    public Func<Assembly, string, bool>? Filter { get; set; }

    /// <summary>
    /// The optional resolver that can extract the catalog name from the <see cref="System.Reflection.Assembly"/> and embedded resource name. Default is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the default behavior is applied.
    /// <para>
    /// The default catalog name extraction behavior is to use the assembly short <see cref="AssemblyName.Name"/> (<c>assembly.GetName().Name)</c>.
    /// </para>
    /// </remarks>
    public Func<Assembly, string, string>? CatalogNameResolver { get; set; }
}