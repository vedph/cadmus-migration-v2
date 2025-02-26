using Cadmus.Core;
using Cadmus.Core.Storage;
using Fusi.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Cadmus.Export;

/// <summary>
/// Default implementation of <see cref="IRendererContext"/>.
/// </summary>
public class RendererContext : DataDictionary, IRendererContext
{
    private readonly ConcurrentDictionary<string, int> _counters = [];

    /// <summary>
    /// Gets or sets the item being rendered.
    /// </summary>
    public IItem? Item { get; set; }

    /// <summary>
    /// Gets the layer part type IDs which are selected for rendering.
    /// </summary>
    public HashSet<string> LayerPartTypeIds { get; } = [];

    /// <summary>
    /// Gets the identifier maps used in this context.
    /// </summary>
    public IDictionary<string, IdMap> IdMaps { get; } =
        new Dictionary<string, IdMap>();

    /// <summary>
    /// Clears this context.
    /// </summary>
    /// <param name="seeds">if set to <c>true</c> also reset the ID maps
    /// seeds.</param>
    public virtual void Clear(bool seeds = false)
    {
        Item = null;
        Data.Clear();

        foreach (IdMap map in IdMaps.Values) map.Reset(seeds);
        _counters.Clear();
        LayerPartTypeIds.Clear();
    }

    /// <summary>
    /// Gets the next autonumber identifier for the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The next autonumber ID.</returns>
    /// <exception cref="ArgumentNullException">key</exception>
    public int GetNextIdFor(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _counters.AddOrUpdate(key, 1, (_, v) => v + 1);
    }

    /// <summary>
    /// Gets the layer IDs dictionary, where keys are block layer ID
    /// prefixes (i.e. part type ID + <c>:</c> + role ID, like
    /// <c>it.vedph.token-text-layer:fr.it.vedph.comment</c>), and
    /// values are target IDs. This mapping is used to build fragment IDs
    /// by mapping each layer to a number unique in the context of each
    /// item. The mapping lasts for all the duration of the item composition
    /// procedure.
    /// </summary>
    [Obsolete]
    public IDictionary<string, int> LayerIds { get; }

    /// <summary>
    /// Gets the fragment IDs dictionary, where keys are block layer IDs
    /// (i.e. part type ID + <c>:</c> + role ID + fragment index, like
    /// <c>it.vedph.token-text-layer:fr.it.vedph.comment0</c>), and
    /// values are the corresponding target elements IDs (i.e.
    /// item number + layer ID + row number + fragment index). The mapping
    /// is scoped to each item.
    /// </summary>
    [Obsolete]
    public IDictionary<string, string> FragmentIds { get; }

    /// <summary>
    /// Gets or sets the optional Cadmus repository to be consumed by
    /// components using this context. Typically this is required by
    /// some filters, like the one resolving thesauri IDs, or the one
    /// extracting text from a fragment's location.
    /// </summary>
    public ICadmusRepository? Repository { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RendererContext"/>
    /// class.
    /// </summary>
    public RendererContext()
    {
        LayerIds = new Dictionary<string, int>();
        FragmentIds = new Dictionary<string, string>();
    }
}
