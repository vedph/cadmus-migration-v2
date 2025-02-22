using Fusi.Cli.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;

namespace Cadmus.Migration.Cli.Services;

/// <summary>
/// Application's context. This includes the Cadmus database connection
/// string, the local assets directory, and an optional logger.
/// </summary>
/// <seealso cref="CliAppContext" />
public class CadmusMigCliAppContext : CliAppContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CadmusMigCliAppContext"/>
    /// class.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public CadmusMigCliAppContext(IConfiguration? config, ILogger? logger)
        : base(config, logger)
    {
    }

    /// <summary>
    /// Gets the context service.
    /// </summary>
    /// <param name="dbName">The database name.</param>
    /// <exception cref="ArgumentNullException">dbName</exception>
    public virtual CadmusMigCliContextService GetContextService(string dbName)
    {
        ArgumentNullException.ThrowIfNull(dbName);

        return new CadmusMigCliContextService(
            new CadmusMigCliContextServiceConfig
            {
                ConnectionString = string.Format(CultureInfo.InvariantCulture,
                    Configuration!.GetConnectionString("Default")!, dbName),
                LocalDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Assets")
            });
    }
}
