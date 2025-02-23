using Cadmus.Core;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using System;

namespace Cadmus.Export.Filters;

/// <summary>
/// A text tree filter which works on "linear" trees, i.e. those trees having
/// a single branch, to split nodes at every occurrence of a LF character.
/// Whenever a node is split, the resulting nodes have the same payload except
/// for the text; the original node text is copied only up to the LF excluded;
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
        string text = node.Data!.Text!;
        int i = text.IndexOf('\n');

        // if no newline found, return the node as is
        if (i == -1) return node;

        // create the first left node
        TreeNode<TextSpanPayload> head = new(node.Data.Clone());
        head.Data!.Text = text[..i];
        head.Data.IsBeforeEol = true;
        TreeNode<TextSpanPayload> current = head;

        // process remaining text
        int start = i + 1;
        while (start < text.Length)
        {
            i = text.IndexOf('\n', start);

            // create new node with the next segment
            TreeNode<TextSpanPayload> next = new(node.Data.Clone());
            if (i == -1)
            {
                // no more newlines, take rest of text
                next.Data!.Text = text[start..];
            }
            else
            {
                // take text up to newline (excluded)
                next.Data!.Text = text[start..i];
                next.Data.IsBeforeEol = true;
            }

            // link nodes
            current.AddChild(next);
            current = next;

            if (i == -1) break;
            start = i + 1;
        }

        return head;
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
                // split node and add to current
                TreeNode<TextSpanPayload> splitHead = SplitNode(node);
                current.AddChild(splitHead);

                // find the last node in the split chain
                current = splitHead;
                while (current.Children.Count > 0)
                    current = current.Children[0];
            }
            else
            {
                // add node as is
                current.AddChild(node);
                current = node;
            }
            return true;
        });

        return root;
    }
}
