namespace Cadmus.Export.Filters;

/// <summary>
/// Renderer filter. This represents any filter applied after the
/// rendition of a Cadmus object.
/// </summary>
public interface IRendererFilter
{
    /// <summary>
    /// Applies this filter to the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="context">The optional rendering context.</param>
    /// <returns>The filtered text.</returns>
    string Apply(string text, IRendererContext? context = null);
}
