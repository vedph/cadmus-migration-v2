using Cadmus.Export.Filters;
using Fusi.Tools.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cadmus.Export.Preview;

/// <summary>
/// Components factory for <see cref="CadmusPreviewer"/>.
/// </summary>
/// <remarks>The JSON configuration has the following sections:
/// <list type="bullet">
/// <item>
/// <term><c>RendererFilters</c></term>
/// <description>List of renderer filters, each named with a key, and having
/// its component ID and eventual options. The key is an arbitrary string,
/// used in the scope of the configuration to reference each filter from
/// other sections.</description>
/// </item>
/// <item>
/// <term><c>JsonRenderers</c></term>
/// <description>List of JSON renderers, each named with a key, and having
/// its component ID and eventual options. The key corresponds to the part
/// type ID, eventually followed by <c>|</c> and its role ID in the case
/// of a layer part. This allows mapping each part type to a specific
/// renderer ID. This key is used in the scope of the configuration to
/// reference each filter from other sections. Under options, any renderer
/// can have a <c>FilterKeys</c> property which is an array of filter keys,
/// representing the filters used by that renderer, to be applied in the
/// specified order.</description>
/// </item>
/// <item>
/// <term><c>TextPartFlatteners</c></term>
/// <description>List of text part flatteners, each named with a key, and
/// having its component ID and eventual options. The key is an arbitrary
/// string, used in the scope of the configuration to reference each filter
/// from other sections.</description>
/// </item>
/// <item>
/// <term><c>TextBlockRenderers</c></term>
/// <description>List of text block renderers, each named with a key, and
/// having its component ID and eventual options. The key is an arbitrary
/// string, used in the scope of the configuration to reference each filter
/// from other sections.</description>
/// </item>
/// <item>
/// <term><c>ItemComposers</c></term>
/// <description>List of item composers, each named with a key, and having
/// its component ID and eventual options. The key is an arbitrary string,
/// not used elsewhere in the context of the configuration. It is used as
/// an argument for UI which process data export. Each composer can have
/// among its options a <c>TextPartFlattenerKey</c> and a
/// <c>TextBlockRendererKey</c>, referencing the corresponding components
/// by their key, and a <c>JsonRendererKeys</c> array, referencing the
/// corresponding JSON renderers by their key.</description>
/// </item>
/// <item>
/// <term><c>ItemIdCollector</c></term>
/// <description>A single item ID collector to use when required. It has
/// the component ID, and eventual options.</description>
/// </item>
/// </list>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="CadmusPreviewFactory" />
/// class.
/// </remarks>
/// <param name="host">The host.</param>
public class CadmusPreviewFactory(IHost host) : ComponentFactory(host)
{
    /// <summary>
    /// The name of the connection string property to be supplied
    /// in POCO option objects (<c>ConnectionString</c>).
    /// </summary>
    public const string CONNECTION_STRING_NAME = "ConnectionString";

    /// <summary>
    /// The optional general connection string to supply to any component
    /// requiring an option named <see cref="CONNECTION_STRING_NAME"/>
    /// (=<c>ConnectionString</c>), when this option is not specified
    /// in its configuration.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Overrides the options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="section">The section.</param>
    protected override void OverrideOptions(object options,
      IConfigurationSection? section)
    {
        Type optionType = options.GetType();

        // if we have a default connection AND the options type
        // has a ConnectionString property, see if we should supply a value
        // for it
        PropertyInfo? property;
        if (ConnectionString != null &&
            (property = optionType.GetProperty(CONNECTION_STRING_NAME)) != null)
        {
            // here we can safely discard the returned object as it will
            // be equal to the input options, which is not null
            SupplyProperty(optionType, property, options, ConnectionString);
        }
    }

    /// <summary>
    /// Configures the container services to use components from
    /// <c>Pythia.Core</c>.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="additionalAssemblies">The optional additional
    /// assemblies.</param>
    /// <exception cref="ArgumentNullException">container</exception>
    public static void ConfigureServices(IServiceCollection services,
        params Assembly[] additionalAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        // https://simpleinjector.readthedocs.io/en/latest/advanced.html?highlight=batch#batch-registration
        Assembly[] assemblies =
        [
            // Cadmus.Export
            typeof(XsltJsonRenderer).Assembly
        ];
        if (additionalAssemblies?.Length > 0)
            assemblies = assemblies.Concat(additionalAssemblies).ToArray();

        // register the components for the specified interfaces
        // from all the assemblies
        foreach (Type it in new[]
        {
            typeof(IJsonRenderer),
            typeof(ITextTreeRenderer),
            typeof(ITextPartFlattener),
            typeof(ITextTreeFilter),
            typeof(ITextTreeRenderer),
            typeof(IRendererFilter),
            typeof(IItemComposer),
            typeof(IItemIdCollector),
        })
        {
            foreach (Type t in GetAssemblyConcreteTypes(assemblies, it))
            {
                services.AddTransient(it, t);
            }
        }
    }

    private HashSet<string> CollectKeys(string collectionPath)
    {
        HashSet<string> keys = [];
        foreach (var entry in
            ComponentFactoryConfigEntry.ReadComponentEntries(
            Configuration, collectionPath)
            .Where(e => e.Keys?.Count > 0))
        {
            foreach (string id in entry.Keys!) keys.Add(id);
        }
        return keys;
    }

    /// <summary>
    /// Gets all the keys registered for JSON renderers in the
    /// configuration of this factory. This is used by client code
    /// to determine for which Cadmus objects a preview is available.
    /// </summary>
    /// <returns>List of unique keys.</returns>
    public HashSet<string> GetJsonRendererKeys()
        => CollectKeys("JsonRenderers");

    /// <summary>
    /// Gets all the keys registered for JSON text part flatteners
    /// in the configuration of this factory. This is used by client code
    /// to determine for which Cadmus objects a preview is available.
    /// </summary>
    /// <returns>List of unique keys.</returns>
    public HashSet<string> GetFlattenerKeys()
        => CollectKeys("TextPartFlatteners");

    /// <summary>
    /// Gets all the keys registered for item composers in the configuration
    /// of this factory.
    /// </summary>
    /// <returns>List of unique keys.</returns>
    public HashSet<string> GetComposerKeys()
        => CollectKeys("ItemComposers");

    private List<IRendererFilter> GetRendererFilters(string path)
    {
        IConfigurationSection filterKeys = Configuration.GetSection(path);
        if (filterKeys.Exists())
        {
            string[] keys = filterKeys.Get<string[]>() ?? [];
            return [.. GetRendererFilters(keys)];
        }
        return [];
    }

    /// <summary>
    /// Gets the JSON renderer with the specified key. The renderer can
    /// specify filters in its <c>Options:FilterKeys</c> array property.
    /// </summary>
    /// <param name="key">The key of the requested renderer.</param>
    /// <returns>Renderer or null if not found.</returns>
    public IJsonRenderer? GetJsonRenderer(string key)
    {
        IList<ComponentFactoryConfigEntry> entries =
            ComponentFactoryConfigEntry.ReadComponentEntries(
            Configuration, "JsonRenderers");

        ComponentFactoryConfigEntry? entry =
            entries.FirstOrDefault(e => e.Keys?.Contains(key) == true);
        if (entry == null) return null;

        IJsonRenderer? renderer = GetComponent<IJsonRenderer>(
            entry.Tag!, entry.OptionsPath);
        if (renderer == null) return null;

        // add filters if specified in Options:FilterKeys
        foreach (IRendererFilter filter in GetRendererFilters(
            entry.OptionsPath + ":FilterKeys"))
        {
            renderer.Filters.Add(filter);
        }

        return renderer;
    }

    /// <summary>
    /// Gets the text tree renderer with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>Renderer or null if not found.</returns>
    public ITextTreeRenderer? GetTextTreeRenderer(string key)
    {
        return GetComponents<ITextTreeRenderer>("TextTreeRenderers",
            null, [key]).FirstOrDefault();
    }

    /// <summary>
    /// Gets the text block renderer with the specified key.
    /// </summary>
    /// <param name="key">The key of the requested renderer.</param>
    /// <returns>Renderer or null if not found.</returns>
    [Obsolete]
    public ITextTreeRenderer? GetTextBlockRenderer(string key)
    {
        IList<ComponentFactoryConfigEntry> entries =
            ComponentFactoryConfigEntry.ReadComponentEntries(
            Configuration, "TextBlockRenderers");

        ComponentFactoryConfigEntry? entry =
            entries.FirstOrDefault(e => e.Keys?.Contains(key) == true);
        if (entry == null) return null;

        ITextTreeRenderer? renderer = GetComponent<ITextTreeRenderer>
            (entry.Tag!, entry.OptionsPath);
        if (renderer == null) return null;

        // add filters if specified in Options:FilterKeys
        foreach (IRendererFilter filter in GetRendererFilters(
            entry.OptionsPath + ":FilterKeys"))
        {
            renderer.Filters.Add(filter);
        }

        return renderer;
    }

    /// <summary>
    /// Gets the text part flattener with the specified key.
    /// </summary>
    /// <param name="key">The key of the requested flattener.</param>
    /// <returns>Flattener or null if not found.</returns>
    public ITextPartFlattener? GetTextPartFlattener(string key)
    {
        return GetComponents<ITextPartFlattener>("TextPartFlatteners",
            null, [key]).FirstOrDefault();
    }

    /// <summary>
    /// Gets the text tree filters matching any of the specified keys.
    /// Filters are listed under section <c>TextTreeFilters</c>, each with
    /// one or more keys.
    /// </summary>
    /// <param name="keys">The keys.</param>
    /// <returns>Dictionary with keys and filters.</returns>
    public IList<ITextTreeFilter> GetTextTreeFilters(IList<string> keys) =>
        GetRequiredComponents<ITextTreeFilter>("TextTreeFilters", null, keys);

    /// <summary>
    /// Gets the JSON renderer filters matching any of the specified keys.
    /// Filters are listed under section <c>RendererFilters</c>, each with
    /// one or more keys.
    /// Then, these keys are used to include post-rendition filters by
    /// listing one or more of them in the <c>FilterKeys</c> option,
    /// an array of strings.
    /// </summary>
    /// <param name="keys">The desired keys.</param>
    /// <returns>Dictionary with keys and filters.</returns>
    public IList<IRendererFilter> GetRendererFilters(IList<string> keys) =>
        GetRequiredComponents<IRendererFilter>("RendererFilters", null, keys);

    /// <summary>
    /// Gets an item composer by key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>Composer or null.</returns>
    /// <exception cref="ArgumentNullException">key</exception>
    public IItemComposer? GetComposer(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        // ItemComposers: match by key
        IList<ComponentFactoryConfigEntry> entries =
            ComponentFactoryConfigEntry.ReadComponentEntries(
            Configuration, "ItemComposers");

        ComponentFactoryConfigEntry? entry =
            entries.FirstOrDefault(e => e.Keys?.Contains(key) == true);
        if (entry == null) return null;

        // instantiate composer
        IItemComposer? composer = GetComponent<IItemComposer>(
            entry.Tag!, entry.OptionsPath);
        if (composer == null) return null;

        // add text part flattener if specified in Options:TextPartFlattenerKey
        IConfigurationSection section = Configuration.GetSection(
            entry.OptionsPath + ":TextPartFlattenerKey");
        if (section.Exists())
        {
            string cKey = section.Get<string>()!;
            composer.TextPartFlattener = GetTextPartFlattener(cKey);
        }

        // add text block renderer if specified in Options:TextBlockRendererKey
        section = Configuration.GetSection(
            entry.OptionsPath + ":TextBlockRendererKey");
        if (section.Exists())
        {
            string cKey = section.Get<string>()!;
            composer.TextTreeRenderer = GetTextBlockRenderer(cKey);
        }

        // add renderers if specified in Options.JsonRendererKeys
        section = Configuration.GetSection(
            entry.OptionsPath + ":JsonRendererKeys");
        if (section.Exists())
        {
            foreach (string cKey in section.Get<string[]>()!)
            {
                IJsonRenderer? renderer = GetJsonRenderer(cKey);
                if (renderer != null) composer.JsonRenderers[cKey] = renderer;
            }
        }

        return composer;
    }

    /// <summary>
    /// Gets the item identifiers collector if any.
    /// </summary>
    /// <returns>The collector defined in this factory configuration,
    /// or null.</returns>
    public IItemIdCollector? GetItemIdCollector() =>
        GetComponent<IItemIdCollector>("ItemIdCollector", false);
}
