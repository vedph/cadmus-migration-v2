using Cadmus.Export.Filters;
using Fusi.Tools.Data;
using System;
using System.Collections.Generic;

namespace Cadmus.Export.ML;

/// <summary>
/// Linear text tree with single apparatus layer renderer.
/// </summary>
/// <seealso cref="ITextTreeRenderer" />
public sealed class AppLinearTextTreeRenderer : ITextTreeRenderer
{
    /// <summary>
    /// Gets the optional filters to apply after the renderer completes.
    /// </summary>
    public IList<IRendererFilter> Filters { get; } = [];

    /// <summary>
    /// Renders the specified tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>Rendition.</returns>
    /// <exception cref="ArgumentNullException">tree or context</exception>
    public string Render(TreeNode<TextSpanPayload> tree, IRendererContext context)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(context);

        tree.Traverse(node =>
        {

            return true;
        });

        throw new NotImplementedException();
    }
}
