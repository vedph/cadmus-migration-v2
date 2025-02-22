using Cadmus.Core;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using System;

namespace Cadmus.Export.Filters;

/// <summary>
/// A text tree filter which works on "linear" trees, i.e. those trees having
/// a single branch, to split nodes at every occurrence of a LF character.
/// Whenever a node is split, the resulting nodes have the same payload except
/// for the text; the original node text is copied only up to the LF included;
/// a new node with text past LF is added if this text is not empty, and this
/// new right-half node becomes the child of the left-half node and the parent
/// of what was the child of the original node.
/// </summary>
/// <seealso cref="ITextTreeFilter" />
[Tag("text-tree-filter.block-linear")]
public sealed class BlockLinearTextTreeFilter : ITextTreeFilter
{
    private static TreeNode<TextSpanPayload> SplitNode(
        TreeNode<TextSpanPayload> node)
    {
        TreeNode<TextSpanPayload>? head = null;
        TreeNode<TextSpanPayload>? current = null;

        string text = node.Data!.Text!;
        int i = text.IndexOf('\n');

        // if ends with a single \n just return the node, no split required
        if (i == text.Length - 1) return node;

        int start = 0;
        while (i > -1)
        {
            // create head node with payload equal to node except for text
            TreeNode<TextSpanPayload> left = new(node.Data.Clone());
            current?.AddChild(left);

            // left = text up to \n included
            left.Data!.Text = text[start..(i + 1)];
            head ??= left;

            // right = text past \n up to the next \n if any; child of left
            TreeNode<TextSpanPayload> right = new(node.Data.Clone());
            int j = text.IndexOf('\n', i + 1);
            right.Data!.Text = j > -1
                ? text[(i + 1)..(j + 1)]
                : text[(i + 1)..];
            left.AddChild(right);

            // move past \n
            current = right;
            i = j > -1? j + 1 : j;
            start = i;
        }

        return head!;
    }

    /// <summary>
    /// Applies this filter to the specified tree, generating a new tree.
    /// </summary>
    /// <param name="tree">The tree's root node.</param>
    /// <param name="item">The item being rendered.</param>
    /// <returns>
    /// The root node of the new tree.
    /// </returns>
    /// <exception cref="ArgumentNullException">tree</exception>
    public TreeNode<TextSpanPayload> Apply(TreeNode<TextSpanPayload> tree,
        IItem item)
    {
        ArgumentNullException.ThrowIfNull(tree);

        TreeNode<TextSpanPayload> root = new();
        TreeNode<TextSpanPayload> current = root;

        tree.Traverse(node =>
        {
            if (node.Data?.Text?.Contains('\n') == true)
            {
                // current -> left -> right
                TreeNode<TextSpanPayload> left = SplitNode(node);
                current.AddChild(left);
                // continue from right
                current = left.Children[0];
            }
            else
            {
                // current -> node
                current.AddChild(node);
                current = node;
            }
            return true;
        });

        return root;
    }
}
