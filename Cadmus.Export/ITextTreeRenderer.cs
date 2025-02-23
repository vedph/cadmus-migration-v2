using Cadmus.Export.Filters;
using Fusi.Tools.Data;

namespace Cadmus.Export;

/// <summary>
/// Renderer of <see cref="TextBlockRow"/>'s.
/// </summary>
public interface ITextTreeRenderer : IHasRendererFilters
{
    /// <summary>
    /// Renders the specified tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>Rendition.</returns>
    string Render(TreeNode<TextSpanPayload> tree, IRendererContext context);
}
