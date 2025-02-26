using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using System;
using System.Text;
using System.Text.Json;

namespace Cadmus.Export.Renderers;

/// <summary>
/// A renderer which outputs a string representing a JSON array where each
/// item is the payload from a node in a linear tree. This is used for single
/// branch trees and the output is typically targeted to frontend UI components.
/// <para>Tag: <c>text-tree-renderer.payload-linear</c>.</para>
/// </summary>
/// <seealso cref="TextTreeRenderer" />
/// <seealso cref="ITextTreeRenderer" />
[Tag("text-tree-renderer.payload-linear")]
public sealed class PayloadLinearTextTreeRenderer : TextTreeRenderer,
    ITextTreeRenderer
{
    /// <summary>
    /// Renders the specified tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>Rendition.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    protected override string DoRender(TreeNode<TextSpanPayload> tree,
        IRendererContext context)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(context);

        if (!tree.HasChildren) return "[]";

        StringBuilder sb = new('[');
        TreeNode<TextSpanPayload>? node = tree.Children[0];
        do
        {
            if (node.Parent != tree) sb.Append(',');
            sb.Append(node.Data != null
                ? JsonSerializer.Serialize(node.Data) : "{}");

            node = node.HasChildren? node.Children[0] : null;
        } while (node != null);

        sb.Append(']');
        return sb.ToString();
    }
}
