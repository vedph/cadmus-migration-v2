using Cadmus.Core;
using Cadmus.Core.Storage;
using Cadmus.Export;
using Cadmus.Export.ML;
using Cadmus.Export.Preview;
using Cadmus.Migration.Cli.Services;
using Fusi.Cli;
using Fusi.Cli.Commands;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Cadmus.Migration.Cli.Commands;

/// <summary>
/// Render items via item composers.
/// </summary>
/// <seealso cref="ICommand" />
internal sealed class RenderItemsCommand : ICommand
{
    private readonly RenderItemsCommandOptions _options;

    private RenderItemsCommand(RenderItemsCommandOptions options)
    {
        _options = options;
    }

    public static void Configure(CommandLineApplication app,
        ICliAppContext context)
    {
        app.Description = "Render a set of items.";
        app.HelpOption("-?|-h|--help");

        CommandArgument dbArgument = app.Argument("[DatabaseName]",
           "The name of the source Cadmus database.");

        CommandArgument cfgPathArgument = app.Argument("[ConfigPath]",
           "The path to the rendering configuration file.");

        CommandOption previewPluginTagOption = app.Option("--preview|-p",
            "The tag of the factory provider plugin",
            CommandOptionType.SingleValue);

        CommandOption repositoryPluginTagOption = app.Option("--repository|-r",
            "The tag of the Cadmus repository plugin",
            CommandOptionType.SingleValue);

        CommandOption composerKeyOption = app.Option("--composer|-c",
            "The key of the item composer to use (default='default').",
            CommandOptionType.SingleValue);

        app.OnExecute(() =>
        {
            context.Command = new RenderItemsCommand(
                new RenderItemsCommandOptions(context)
                {
                    DatabaseName = dbArgument.Value,
                    ConfigPath = cfgPathArgument.Value,
                    PreviewFactoryProviderTag = previewPluginTagOption.Value(),
                    RepositoryProviderTag = repositoryPluginTagOption.Value(),
                    ComposerKey = composerKeyOption.Value() ?? "default",
                });
            return 0;
        });
    }

    public Task<int> Run()
    {
        ColorConsole.WriteWrappedHeader("Render Items",
            headerColor: ConsoleColor.Green);
        Console.WriteLine($"Database: {_options.DatabaseName}");
        Console.WriteLine($"Config path: {_options.ConfigPath}");
        Console.WriteLine("Factory provider tag: " +
            $"{_options.PreviewFactoryProviderTag ?? "---"}");
        Console.WriteLine("Repository provider tag: " +
            $"{_options.RepositoryProviderTag ?? "---"}");
        Console.WriteLine($"Composer key: {_options.ComposerKey}\n");

        string cs = string.Format(
            _options.Configuration!.GetConnectionString("Default")!,
            _options.DatabaseName);

        CadmusMigCliContextService contextService =
            _options.Context.GetContextService(_options.DatabaseName);

        // load rendering config
        ColorConsole.WriteInfo("Loading rendering config...");
        string config = CommandHelper.LoadFileContent(_options.ConfigPath!);

        // get preview factory from its provider
        ColorConsole.WriteInfo("Building preview factory...");
        ICadmusPreviewFactoryProvider? provider =
            CadmusMigCliContextService.GetPreviewFactoryProvider(
                _options.PreviewFactoryProviderTag);
        if (provider == null)
        {
            ColorConsole.WriteError("Preview factory provider not found");
            return Task.FromResult(2);
        }
        CadmusPreviewFactory factory = provider.GetFactory(config,
            typeof(FSTeiStandoffItemComposer).Assembly);
        factory.ConnectionString = cs;

        // get the Cadmus repository from the specified plugin
        ColorConsole.WriteInfo("Building repository factory...");
        ICadmusRepository repository = contextService.GetCadmusRepository(
            _options.RepositoryProviderTag!);
        if (repository == null)
        {
            throw new InvalidOperationException(
                "Unable to create Cadmus repository");
        }

        // create the preview item composer
        ColorConsole.WriteInfo("Creating item composer...");
        IItemComposer? composer = factory.GetComposer(_options.ComposerKey);
        if (composer == null)
        {
            ColorConsole.WriteError(
                $"Could not find composer with key {_options.ComposerKey}. " +
                "Please check your rendering configuration.");
            return Task.FromResult(2);
        }

        // create ID collector
        ColorConsole.WriteInfo("Creating item collector...");
        IItemIdCollector? collector = factory.GetItemIdCollector();
        if (collector == null)
        {
            ColorConsole.WriteError(
                "No item ID collector defined in configuration.");
            return Task.FromResult(2);
        }

        // render items
        ColorConsole.WriteInfo("Rendering items...");

        composer.Open();
        foreach (string id in collector.GetIds())
        {
            ColorConsole.WriteInfo(" - " + id);
            IItem? item = repository.GetItem(id, true);
            if (item != null)
            {
                ColorConsole.WriteInfo("   " + item.Title);
                composer.Compose(item);
            }
        }
        composer.Close();

        return Task.FromResult(0);
    }
}

internal class RenderItemsCommandOptions :
    CommandOptions<CadmusMigCliAppContext>
{
    /// <summary>
    /// Gets or sets the tag of the component found in some plugin and
    /// implementing <see cref="ICadmusPreviewFactoryProvider"/>.
    /// </summary>
    public string? PreviewFactoryProviderTag { get; set; }

    /// <summary>
    /// Gets or sets the tag of the component found in some plugin and
    /// implementing Cadmus <see cref="IRepositoryProvider"/>.
    /// </summary>
    public string? RepositoryProviderTag { get; set; }

    /// <summary>
    /// Gets or sets the name of the database to read items from.
    /// </summary>
    public string DatabaseName { get; set; }

    /// <summary>
    /// Gets or sets the path to the rendering configuration file.
    /// </summary>
    public string ConfigPath { get; set; }

    /// <summary>
    /// Gets or sets the key in the rendering configuration file for the
    /// item composer to use.
    /// </summary>
    public string ComposerKey { get; set; }

    public RenderItemsCommandOptions(ICliAppContext options)
        : base((CadmusMigCliAppContext)options)
    {
        DatabaseName = "cadmus";
        ConfigPath = "render.config";
        ComposerKey = "default";
    }
}