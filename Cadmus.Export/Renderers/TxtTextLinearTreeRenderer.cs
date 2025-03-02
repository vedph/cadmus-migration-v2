using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using System;
using System.Text;

namespace Cadmus.Export.Renderers;

/// <summary>
/// Plain text linear tree renderer. This renderer outputs a plain text
/// from a linear tree. The text is obtained by concatenating the text
/// of each node in the tree, optionally adding a newline after each node
/// having the <c>IsBeforeEol</c> flag set to true.
/// </summary>
/// <seealso cref="TextTreeRenderer" />
/// <seealso cref="ITextTreeRenderer" />
[Tag("it.vedph.text-tree-renderer.txt-linear")]
public sealed class TxtTextLinearTreeRenderer : TextTreeRenderer,
    ITextTreeRenderer
{
    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="tree">The root node of the text tree.</param>
    /// <param name="context">The renderer context.</param>
    /// <returns>
    /// Rendered output.
    /// </returns>
    /// <exception cref="ArgumentNullException">tree or context</exception>
    protected override string DoRender(TreeNode<TextSpan> tree,
        IRendererContext context)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(context);

        StringBuilder text = new();
        tree.Traverse(node =>
        {
            if (!string.IsNullOrEmpty(node.Data?.Text))
                text.Append(node.Data.Text);

            if (node.Data?.IsBeforeEol == true)
                text.AppendLine();

            return true;
        });

        return text.ToString();
    }
}
