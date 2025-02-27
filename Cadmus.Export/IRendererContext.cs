using Cadmus.Core;
using Cadmus.Core.Storage;
using Fusi.Tools;
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
    /// Gets the mapped ID for the specified source ID.
    /// </summary>
    /// <param name="map">The ID of the map to use.</param>
    /// <param name="id">The identifier to find the mapped ID of.</param>
    /// <returns>Numeric ID or null if not found.</returns>
    int? GetMappedId(string map, string id);

    /// <summary>
    /// Gets the source identifier from its mapped unique number.
    /// </summary>
    /// <param name="map">The ID of the map to use.</param>
    /// <param name="id">The mapped number.</param>
    /// <returns>The source identifier or null if not found.</returns>
    string? GetSourceId(string map, int id);

    /// <summary>
    /// Gets the text part from <see cref="Item"/> if any.
    /// </summary>
    /// <returns>Text part or null.</returns>
    IPart? GetTextPart();
}
