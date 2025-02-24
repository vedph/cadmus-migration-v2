using System;
using System.Collections.Generic;
using Cadmus.Export.Filters;
using Fusi.Tools.Data;

namespace Cadmus.Export;

/// <summary>
/// Base class for <see cref="ITextTreeRenderer"/> implementations.
/// </summary>
public abstract class TextTreeRenderer : IHasRendererFilters
{
    /// <summary>
    /// Gets the optional filters to apply after the renderer completes.
    /// </summary>
    public IList<IRendererFilter> Filters { get; } = [];

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="tree">The root node of the text tree.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>Rendered output.</returns>
    protected abstract string DoRender(TreeNode<TextSpanPayload> tree,
        IRendererContext? context = null);

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="tree">The root node of the text tree.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>Rendered output.</returns>
    /// <exception cref="ArgumentNullException">tree</exception>
    public string Render(TreeNode<TextSpanPayload> tree,
        IRendererContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(tree);

        string result = DoRender(tree, context);

        // apply filters
        foreach (IRendererFilter filter in Filters)
            result = filter.Apply(result, context);

        return result;
    }
}
