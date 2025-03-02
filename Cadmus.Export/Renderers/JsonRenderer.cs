using System;
using System.Collections.Generic;
using Cadmus.Export.Filters;
using Fusi.Tools.Data;

namespace Cadmus.Export.Renderers;

/// <summary>
/// Base class for <see cref="IJsonRenderer"/>'s.
/// </summary>
public abstract class JsonRenderer
{
    /// <summary>
    /// Gets the optional filters to apply after the renderer completes.
    /// </summary>
    public IList<IRendererFilter> Filters { get; init; } = [];

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="json">The input JSON.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <param name="tree">The optional text tree. This is used for layer
    /// fragments to get source IDs targeting the various portions of the
    /// text.</param>
    /// <returns>Rendered output.</returns>
    protected abstract string DoRender(string json,
        IRendererContext context,
        TreeNode<TextSpan>? tree = null);

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="json">The input JSON.</param>
    /// <param name="context">The renderer context.</param>
    /// <returns>Rendered output.</returns>
    /// <param name="tree">The optional text tree. This is used for layer
    /// fragments to get source IDs targeting the various portions of the
    /// text.</param>
    /// <exception cref="ArgumentNullException">json or context</exception>
    public string Render(string json, IRendererContext context,
        TreeNode<TextSpan>? tree = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(context);

        string result = DoRender(json, context, tree);

        if (Filters.Count > 0)
        {
            foreach (IRendererFilter filter in Filters)
                result = filter.Apply(result, context);
        }

        return result;
    }
}
