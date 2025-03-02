using Cadmus.Core;
using Cadmus.Export.Filters;
using Fusi.Tools.Data;

namespace Cadmus.Export;

/// <summary>
/// Renderer of text trees.
/// </summary>
public interface ITextTreeRenderer : IHasRendererFilters
{
    /// <summary>
    /// Resets the state of this renderer if any. This is called once before
    /// starting the rendering process.
    /// </summary>
    /// <param name="context">The context.</param>
    void Reset(IRendererContext context);

    /// <summary>
    /// Renders the head of the output. This is called by the item composer
    /// once when starting the rendering process and can be used to output
    /// specific content at the document's start.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>Head content.</returns>
    string RenderHead(IRendererContext context);

    /// <summary>
    /// Called when items group has changed.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="prevGroupId">The previous group identifier.</param>
    /// <param name="context">The context.</param>
    void OnGroupChanged(IItem item, string? prevGroupId, IRendererContext context);

    /// <summary>
    /// Renders the specified tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>Rendition.</returns>
    string Render(TreeNode<TextSpan> tree, IRendererContext context);

    /// <summary>
    /// Renders the tail of the output. This is called by the item composer
    /// once when ending the rendering process and can be used to output
    /// specific content at the document's end.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>Tail content.</returns>
    string RenderTail(IRendererContext context);
}
