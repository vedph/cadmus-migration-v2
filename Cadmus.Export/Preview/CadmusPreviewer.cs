using Cadmus.Core;
using Cadmus.Core.Storage;
using Fusi.Xml.Extras.Scan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Cadmus.Export.Preview;

/// <summary>
/// Cadmus object previewer. This is a high level class using rendition
/// components to render a preview for Cadmus parts or fragments.
/// </summary>
public sealed class CadmusPreviewer
{
    private readonly ICadmusRepository? _repository;
    private readonly CadmusPreviewFactory _factory;
    private TextBlockBuilder? _blockBuilder;
    // cache
    private readonly Dictionary<string, IJsonRenderer> _jsonRenderers;
    private readonly Dictionary<string, ITextPartFlattener> _flatteners;

    /// <summary>
    /// Initializes a new instance of the <see cref="CadmusPreviewer"/> class.
    /// </summary>
    /// <param name="factory">The factory.</param>
    /// <param name="repository">The optional repository. You should always
    /// pass a repository, unless you are just consuming the methods using
    /// JSON as their input.</param>
    /// <exception cref="ArgumentNullException">repository or factory</exception>
    public CadmusPreviewer(CadmusPreviewFactory factory,
        ICadmusRepository? repository)
    {
        _factory = factory ??
            throw new ArgumentNullException(nameof(factory));
        _repository = repository;

        // cached components
        _jsonRenderers = [];
        _flatteners = [];
    }

    /// <summary>
    /// Gets all the keys registered for JSON renderers in the
    /// configuration of this factory. This is used by client code
    /// to determine for which Cadmus objects a preview is available.
    /// </summary>
    /// <returns>List of unique keys.</returns>
    public HashSet<string> GetJsonRendererKeys()
        => _factory.GetJsonRendererKeys();

    /// <summary>
    /// Gets all the keys registered for JSON text part flatteners
    /// in the configuration of this factory. This is used by client code
    /// to determine for which Cadmus objects a preview is available.
    /// </summary>
    /// <returns>List of unique keys.</returns>
    public HashSet<string> GetFlattenerKeys()
        => _factory.GetFlattenerKeys();

    /// <summary>
    /// Gets all the keys registered for item composers in the configuration
    /// of this factory.
    /// </summary>
    /// <returns>List of unique keys.</returns>
    public HashSet<string> GetComposerKeys()
        => _factory.GetComposerKeys();

    private IJsonRenderer? GetRendererFromKey(string key)
    {
        IJsonRenderer? renderer;

        if (_jsonRenderers.ContainsKey(key))
        {
            renderer = _jsonRenderers[key];
        }
        else
        {
            renderer = _factory.GetJsonRenderer(key);
            if (renderer == null) return null;
            _jsonRenderers[key] = renderer;
        }
        return renderer;
    }

    private IRendererContext? BuildContext(IItem? item)
    {
        if (item == null) return null;

        RendererContext context = new()
        {
            Repository = _repository
        };

        context.Data[ItemComposer.M_ITEM_ID] = item.Id;
        context.Data[ItemComposer.M_ITEM_FACET] = item.FacetId;

        if (!string.IsNullOrEmpty(item.GroupId))
            context.Data[ItemComposer.M_ITEM_GROUP] = item.GroupId;

        context.Data[ItemComposer.M_ITEM_TITLE] = item.Title;

        if (item.Flags != 0)
            context.Data[ItemComposer.M_ITEM_FLAGS] = item.Flags;

        return context;
    }

    /// <summary>
    /// Renders the JSON code representing a part.
    /// </summary>
    /// <param name="json">The JSON code representing the part's content.</param>
    /// <returns>Rendition or empty string.</returns>
    /// <param name="context">The optional renderer context.</param>
    /// <exception cref="ArgumentNullException">json</exception>
    public string RenderPartJson(string json, IRendererContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(json);

        // get part type ID
        JsonDocument doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("typeId", out JsonElement typeIdElem))
            return "";
        string? typeId = typeIdElem.GetString();
        if (typeId == null) return "";

        // get the renderer targeting the part type ID
        IJsonRenderer? renderer = GetRendererFromKey(typeId);

        // render
        return renderer != null ? renderer.Render(json, context) : "";
    }

    /// <summary>
    /// Renders the part with the specified ID, using the renderer targeting
    /// its part type ID.
    /// Note that this method requires a repository.
    /// </summary>
    /// <param name="itemId">The item's identifier. This is used to get
    /// item's metadata, eventually consumed by filters. If there is no
    /// repository, or the item is not found, no context will be created
    /// and passed to filters.</param>
    /// <param name="partId">The part's identifier.</param>
    /// <returns>Rendition or empty string.</returns>
    /// <exception cref="ArgumentNullException">itemId or partId</exception>
    public string RenderPart(string itemId, string partId)
    {
        ArgumentNullException.ThrowIfNull(itemId);
        ArgumentNullException.ThrowIfNull(partId);

        IItem? item = _repository?.GetItem(itemId, false);
        IRendererContext? context = BuildContext(item);

        string? json = _repository?.GetPartContent(partId);
        if (json == null) return "";

        return RenderPartJson(json, context);
    }

    private static JsonElement? GetFragmentAt(JsonElement fragments, int index)
    {
        if (index >= fragments.GetArrayLength()) return null;

        int i = 0;
        foreach (JsonElement fr in fragments.EnumerateArray())
        {
            if (i == index) return fr;
            i++;
        }
        return null;
    }

    /// <summary>
    /// Renders the specified fragment's JSON, representing a layer part.
    /// </summary>
    /// <param name="json">The JSON code representing the layer part's
    /// content.</param>
    /// <param name="frIndex">Index of the fragment in the <c>fragments</c>
    /// array of the received layer part.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>Rendition or empty string.</returns>
    /// <exception cref="ArgumentNullException">json</exception>
    public string RenderFragmentJson(string json, int frIndex,
        IRendererContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(json);

        // get the part type ID and role ID (=fragment type)
        JsonDocument doc = JsonDocument.Parse(json);

        // type
        if (!doc.RootElement.TryGetProperty("typeId",
            out JsonElement typeIdElem))
        {
            return "";
        }
        string? typeId = typeIdElem.GetString();
        if (typeId == null) return "";

        // role
        if (!doc.RootElement.TryGetProperty("roleId",
            out JsonElement roleIdElem))
        {
            return "";
        }
        string? roleId = roleIdElem.GetString();
        if (roleId == null) return "";

        // the target ID is the combination of these two IDs
        string key = $"{typeId}|{roleId}";

        IJsonRenderer? renderer = GetRendererFromKey(key);

        // extract the targeted fragment
        if (!doc.RootElement.TryGetProperty("fragments",
            out JsonElement fragments))
        {
            return "";
        }
        JsonElement? fr = GetFragmentAt(fragments, frIndex);
        if (fr == null) return "";

        // render
        string frJson = fr.ToString()!;
        return renderer != null ? renderer.Render(frJson, context) : "";
    }

    /// <summary>
    /// Renders the fragment at index <paramref name="frIndex"/> in the part
    /// with ID <paramref name="partId"/>, using the renderer targeting
    /// its part role ID.
    /// Note that this method requires a repository.
    /// </summary>
    /// <param name="itemId">The item's identifier. This is used to get
    /// item's metadata, eventually consumed by filters. If there is no
    /// repository, or the item is not found, no context will be created
    /// and passed to filters.</param>
    /// <param name="partId">The part's identifier.</param>
    /// <returns>Rendition or empty string.</returns>
    /// <param name="frIndex">The fragment's index in the layer part's
    /// fragments array.</param>
    /// <exception cref="ArgumentNullException">itemId or partId</exception>
    /// <exception cref="ArgumentOutOfRangeException">frIndex less than 0
    /// </exception>
    public string RenderFragment(string itemId, string partId, int frIndex)
    {
        ArgumentNullException.ThrowIfNull(itemId);
        ArgumentNullException.ThrowIfNull(partId);
        if (frIndex < 0) throw new ArgumentOutOfRangeException(nameof(frIndex));

        string? json = _repository?.GetPartContent(partId);
        if (json == null) return "";

        IItem? item = _repository?.GetItem(itemId, false);

        IRendererContext? context = BuildContext(item);

        return RenderFragmentJson(json, frIndex, context);
    }

    /// <summary>
    /// Builds the text blocks from the specified text part.
    /// Note that this method requires a repository.
    /// </summary>
    /// <param name="id">The part identifier.</param>
    /// <param name="layerPartIds">The IDs of the layers to include in the
    /// rendition.</param>
    /// <param name="layerIds">The optional IDs to assign to each layer
    /// part's range. When specified, it must have the same size of
    /// <paramref name="layerPartIds"/> so that the first entry in it
    /// corresponds to the first entry in layer IDs, the second to the second,
    /// and so forth.</param>
    /// <returns>Rendition.</returns>
    /// <exception cref="ArgumentNullException">id or layerIds</exception>
    public IList<TextBlockRow> BuildTextBlocks(string id,
        IList<string> layerPartIds, IList<string?>? layerIds = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(layerPartIds);

        if (_repository == null) return Array.Empty<TextBlockRow>();

        string? json = _repository.GetPartContent(id);
        if (json == null) return Array.Empty<TextBlockRow>();

        // get the part type ID (role ID is always base-text)
        JsonDocument doc = JsonDocument.Parse(json);
        string? typeId = doc.RootElement.GetProperty("typeId").GetString();
        if (typeId == null) return Array.Empty<TextBlockRow>();

        // get the flattener for that type ID
        ITextPartFlattener? flattener;
        if (_flatteners.ContainsKey(typeId))
        {
            flattener = _flatteners[typeId];
        }
        else
        {
            flattener = _factory.GetTextPartFlattener(typeId);
            if (flattener == null) return Array.Empty<TextBlockRow>();
            _flatteners[typeId] = flattener;
        }

        // load part and layers
        IPart? part = _repository?.GetPart<IPart>(id);
        if (part == null) return Array.Empty<TextBlockRow>();
        List<IPart> layerParts = layerPartIds
            .Select(lid => _repository!.GetPart<IPart>(lid)!)
            .Where(p => p != null)
            .ToList();

        // flatten them
        var tr = flattener.GetTextRanges(part, layerParts);

        // build blocks rows
        // TODO: implement
        return [];
        //if (_blockBuilder == null) _blockBuilder = new();
        //return _blockBuilder.Build(tr.Item1, tr.Item2).ToList();
    }
}
