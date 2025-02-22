using Cadmus.Cli.Core;
using Cadmus.Core.Storage;
using Cadmus.Core;
using Cadmus.Export.Preview;
using System.IO;
using System;

namespace Cadmus.Migration.Cli.Services;

/// <summary>
/// CLI context service.
/// </summary>
public sealed class CadmusMigCliContextService
{
    private readonly CadmusMigCliContextServiceConfig _config;

    public CadmusMigCliContextService(CadmusMigCliContextServiceConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Gets the preview factory provider with the specified plugin tag
    /// (assuming that the plugin has a (single) implementation of
    /// <see cref="ICadmusPreviewFactoryProvider"/>).
    /// </summary>
    /// <param name="pluginTag">The tag of the component in its plugin,
    /// or null to use the standard preview factory provider.</param>
    /// <returns>The provider.</returns>
    public static ICadmusPreviewFactoryProvider? GetPreviewFactoryProvider(
        string? pluginTag = null)
    {
        if (pluginTag == null)
            return new StandardPreviewFactoryProvider();

        return PluginFactoryProvider
            .GetFromTag<ICadmusPreviewFactoryProvider>(pluginTag);
    }

    /// <summary>
    /// Gets the cadmus repository.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <returns>Repository</returns>
    /// <exception cref="FileNotFoundException">Repository provider not
    /// found.</exception>
    public ICadmusRepository GetCadmusRepository(string? tag)
    {
        IRepositoryProvider? provider = PluginFactoryProvider
            .GetFromTag<IRepositoryProvider>(tag);
        if (provider == null)
        {
            throw new FileNotFoundException(
                "The requested repository provider tag " + tag +
                " was not found among plugins in " +
                PluginFactoryProvider.GetPluginsDir());
        }
        provider.ConnectionString = _config.ConnectionString!;
        return provider.CreateRepository();
    }
}

/// <summary>
/// Configuration for <see cref="CadmusMigCliContextService"/>.
/// </summary>
public class CadmusMigCliContextServiceConfig
{
    /// <summary>
    /// Gets or sets the connection string to the database.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the local directory to use when loading resources
    /// from the local file system.
    /// </summary>
    public string? LocalDirectory { get; set; }
}