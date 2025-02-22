using System.Reflection;

namespace Cadmus.Export.Preview;

/// <summary>
/// Provider for <see cref="CadmusPreviewFactory"/>.
/// </summary>
public interface ICadmusPreviewFactoryProvider
{
    /// <summary>
    /// Gets the factory.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="additionalAssemblies">The optional additional assemblies
    /// to load components from.</param>
    /// <returns>Factory.</returns>
    CadmusPreviewFactory GetFactory(string profile,
        params Assembly[] additionalAssemblies);
}
