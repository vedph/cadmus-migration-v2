using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Fusi.Tools.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Cadmus.Export.Preview;

/// <summary>
/// Standard preview factory provider.
/// </summary>
/// <seealso cref="ICadmusPreviewFactoryProvider" />
[Tag("it.vedph.preview-factory-provider.standard")]
public sealed class StandardPreviewFactoryProvider : ICadmusPreviewFactoryProvider
{
    private static IHost GetHost(string config, Assembly[] assemblies)
    {
        return new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                CadmusPreviewFactory.ConfigureServices(services, assemblies);
            })
            // extension method from Fusi library
            .AddInMemoryJson(config)
            .Build();
    }

    /// <summary>
    /// Gets the factory.
    /// </summary>
    /// <param name="profile">The JSON configuration profile.</param>
    /// <param name="additionalAssemblies">The optional additional assemblies
    /// to load components from.</param>
    /// <returns>Factory.</returns>
    public CadmusPreviewFactory GetFactory(string profile,
        params Assembly[] additionalAssemblies)
    {
        return new CadmusPreviewFactory(GetHost(profile, additionalAssemblies));
    }
}
