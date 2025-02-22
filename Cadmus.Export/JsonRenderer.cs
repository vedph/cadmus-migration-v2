using System.Collections.Generic;
using Cadmus.Export.Filters;

namespace Cadmus.Export;

/// <summary>
/// Base class for <see cref="IJsonRenderer"/>'s.
/// </summary>
public abstract class JsonRenderer
{
    /// <summary>
    /// Gets the optional filters to apply after the renderer completes.
    /// </summary>
    public IList<IRendererFilter> Filters { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRenderer"/> class.
    /// </summary>
    protected JsonRenderer()
    {
        Filters = new List<IRendererFilter>();
    }

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="json">The input JSON.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>Rendered output.</returns>
    protected abstract string DoRender(string json,
        IRendererContext? context = null);

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="json">The input JSON.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>Rendered output.</returns>
    public string Render(string json, IRendererContext? context = null)
    {
        if (string.IsNullOrEmpty(json)) return json;

        string result = DoRender(json, context);

        if (Filters.Count > 0)
        {
            foreach (IRendererFilter filter in Filters)
                result = filter.Apply(result, context);
        }

        return result;
    }
}
