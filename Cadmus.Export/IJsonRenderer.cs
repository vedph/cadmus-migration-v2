using Cadmus.Export.Filters;

namespace Cadmus.Export;

/// <summary>
/// Renderer for any object represented by JSON (like a part or a fragment).
/// This takes as input the JSON code, and renders it into some output format.
/// </summary>
public interface IJsonRenderer : IHasRendererFilters
{
    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="json">The input JSON.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>Rendered output.</returns>
    string Render(string json, IRendererContext? context = null);
}
