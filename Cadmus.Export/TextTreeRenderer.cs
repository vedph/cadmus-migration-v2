using System;
using System.Collections.Generic;
using Cadmus.Core;
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
    /// Resets the state of this renderer if any. This is called once before
    /// starting the rendering process.
    /// </summary>
    /// <param name="context">The context.</param>
    public virtual void Reset(IRendererContext context)
    {
        // nothing to do, override if required
    }

    /// <summary>
    /// Renders the head of the output. This is called by the item composer
    /// once when starting the rendering process and can be used to output
    /// specific content at the document's start.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>Head content.</returns>
    public virtual string RenderHead(IRendererContext context)
    {
        return "";
    }

    /// <summary>
    /// Called when items group has changed. The default implementation does
    /// nothing.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="prevGroupId">The previous group identifier.</param>
    /// <param name="context">The context.</param>
    public virtual void OnGroupChanged(IItem item, string? prevGroupId,
        IRendererContext context)
    {
        // nothing to do, override if required
    }

    /// <summary>
    /// Renders the tail of the output. This is called by the item composer
    /// once when ending the rendering process and can be used to output
    /// specific content at the document's end.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>Tail content.</returns>
    public virtual string RenderTail(IRendererContext context)
    {
        return "";
    }

    /// <summary>
    /// Gets the next identifier for the specified key. The identifier is
    /// used to build progressively unique identifiers for some rendered
    /// elements.
    /// </summary>
    /// <param name="key">The identifier key.</param>
    /// <param name="context">The context to use.</param>
    /// <returns>ID value.</returns>
    protected static int GetNextIdFor(string key, IRendererContext context)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Data.TryGetValue(key, out object? value))
        {
            context.Data[key] = 1;
            return 1;
        }
        int id = (int)value;
        context.Data[key] = id + 1;
        return id;
    }

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="tree">The root node of the text tree.</param>
    /// <param name="context">The renderer context.</param>
    /// <returns>Rendered output.</returns>
    protected abstract string DoRender(TreeNode<TextSpanPayload> tree,
        IRendererContext context);

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="tree">The root node of the text tree.</param>
    /// <param name="context">The renderer context.</param>
    /// <returns>Rendered output.</returns>
    /// <exception cref="ArgumentNullException">tree</exception>
    public string Render(TreeNode<TextSpanPayload> tree,
        IRendererContext context)
    {
        ArgumentNullException.ThrowIfNull(tree);

        string result = DoRender(tree, context);

        // apply filters
        foreach (IRendererFilter filter in Filters)
            result = filter.Apply(result, context);

        return result;
    }
}
