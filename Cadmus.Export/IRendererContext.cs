using Cadmus.Core;
using Cadmus.Core.Storage;
using Fusi.Tools;
using System;
using System.Collections.Generic;

namespace Cadmus.Export;

/// <summary>
/// Renderer context. This includes a generic metadata dictionary and
/// a couple of maps for handling text layers.
/// </summary>
/// <seealso cref="IHasDataDictionary" />
public interface IRendererContext : IHasDataDictionary
{
    /// <summary>
    /// Gets or sets the item being rendered.
    /// </summary>
    IItem? Item { get; set; }

    /// <summary>
    /// Gets the layer part type IDs which are selected for rendering.
    /// </summary>
    HashSet<string> LayerPartTypeIds { get; }

    /// <summary>
    /// Gets the identifier maps used in this context.
    /// </summary>
    IDictionary<string, IdMap> IdMaps { get; }

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
    IDictionary<string, int> LayerIds { get; }

    /// <summary>
    /// Gets the fragment IDs dictionary, where keys are block layer IDs
    /// (i.e. part type ID + <c>:</c> + role ID + fragment index, like
    /// <c>it.vedph.token-text-layer:fr.it.vedph.comment0</c>), and
    /// values are the corresponding target elements IDs (i.e.
    /// item number + layer ID + row number + fragment index). The mapping
    /// is scoped to each item.
    /// </summary>
    [Obsolete]
    IDictionary<string, string> FragmentIds { get; }

    /// <summary>
    /// Gets or sets the optional Cadmus repository to be consumed by
    /// components using this context. Typically this is required by
    /// some filters, like the one resolving thesauri IDs, or the one
    /// extracting text from a fragment's location.
    /// </summary>
    ICadmusRepository? Repository { get; set; }

    /// <summary>
    /// Clears this context.
    /// </summary>
    /// <param name="seeds">if set to <c>true</c> also reset the ID maps
    /// seeds.</param>
    void Clear(bool seeds = false);

    /// <summary>
    /// Gets the next autonumber identifier for the category specified by
    /// <paramref name="categoryKey"/>. This is just a progressive number starting
    /// from 1.
    /// </summary>
    /// <param name="categoryKey">The key.</param>
    /// <returns>The next autonumber ID.</returns>
    int GetNextIdFor(string categoryKey);

    /// <summary>
    /// Maps the specified source (e.g. fragment etc.) global identifier
    /// into a number. This is idempotent, i.e. if the ID has already been
    /// mapped, the same number is returned.
    /// </summary>
    /// <param name="map">The ID of the map to use.</param>
    /// <param name="id">The identifier to map.</param>
    /// <returns>Numeric ID.</returns>
    int MapSourceId(string map, string id);

    /// <summary>
    /// Gets the source identifier from its mapped unique number.
    /// </summary>
    /// <param name="map">The ID of the map to use.</param>
    /// <param name="id">The mapped number.</param>
    /// <returns>The source identifier or null if not found.</returns>
    string? GetSourceId(string map, int id);
}
