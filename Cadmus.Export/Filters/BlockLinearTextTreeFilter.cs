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
/// <para>Tag: <c>it.vedph.text-tree-filter.block-linear</c>.</para>
/// </summary>
/// <seealso cref="ITextTreeFilter" />
[Tag("it.vedph.text-tree-filter.block-linear")]
public sealed class BlockLinearTextTreeFilter : ITextTreeFilter
{
    /// <summary>
    /// Splits a node at newline characters, handling all newlines in the text.
    /// </summary>
    /// <param name="node">The node to split.</param>
    /// <param name="skipInitialNewline">True to skip an initial newline if 
    /// present.</param>
    /// <returns>The head of the split chain.</returns>
    private static TreeNode<TextSpanPayload> SplitNode(
        TreeNode<TextSpanPayload> node,
        bool skipInitialNewline = false)
    {
        string? text = node.Data!.Text;
        if (string.IsNullOrEmpty(text)) return node;

        int startIndex = skipInitialNewline && text[0] == '\n' ? 1 : 0;
        if (startIndex >= text.Length) return node;

        // find the first newline (after the skipped one if applicable)
        int i = text.IndexOf('\n', startIndex);

        // if no newline found, return the node with appropriate text
        if (i == -1)
        {
            if (startIndex == 0) return node;

            // just skip the initial newline
            TreeNode<TextSpanPayload> result = new(node.Data.Clone());
            result.Data!.Text = text[startIndex..];
            return result;
        }

        // create the first node with text up to the newline
        TreeNode<TextSpanPayload> head = new(node.Data.Clone());
        head.Data!.Text = startIndex == 0 ? text[..i] : text[startIndex..i];
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
    /// Assigns IDs to all the nodes not having one, using an autonumber value
    /// starting from the max ID in the tree + 1. This is used to assign IDs
    /// to newly created nodes, thus ensuring that all the nodes have one and
    /// IDs are unique within the tree.
    /// </summary>
    /// <param name="root">The root.</param>
    private static void AssignNodeIds(TreeNode<TextSpanPayload> root)
    {
        // first pass gets max node ID
        int maxId = 0;
        root.Traverse(node =>
        {
            if (!string.IsNullOrEmpty(node.Id))
            {
                int n = int.Parse(node.Id);
                if (maxId < n) maxId = n;
            }
            return true;
        });

        // assign IDs to all the nodes without it
        root.Traverse(node =>
        {
            if (string.IsNullOrEmpty(node.Id))
                node.Id = $"{++maxId}";
            return true;
        });
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
            if (node.Data?.Text == null || node == tree) return true;

            // handle nodes with newlines
            if (node.Data.Text.Contains('\n'))
            {
                // case 1: single newline only - mark parent and skip
                if (node.Data.Text == "\n")
                {
                    if (current.Data != null) current.Data.IsBeforeEol = true;
                    return true;
                }

                // case 2: text starts with newline - mark parent and handle
                // content after newline
                if (node.Data.Text.StartsWith('\n'))
                {
                    // mark parent (if it's root we need to create its payload)
                    if (current.Data != null) current.Data.IsBeforeEol = true;
                    else current.Data = new TextSpanPayload { IsBeforeEol = true };

                    if (node.Data.Text.Length > 1)
                    {
                        // Process the text after the initial newline
                        TreeNode<TextSpanPayload> head = SplitNode(node, true);
                        current.AddChild(head);

                        // Move to the last node in the chain
                        current = head;
                        while (current.Children.Count > 0)
                            current = current.Children[0];
                    }

                    return true;
                }

                // case 3: Regular case - newlines in the middle or at end
                TreeNode<TextSpanPayload> splitHead = SplitNode(node);
                current.AddChild(splitHead);

                // move to the last node in the chain
                current = splitHead;
                while (current.Children.Count > 0)
                    current = current.Children[0];
            }
            // handle nodes without newlines
            else
            {
                TreeNode<TextSpanPayload> child = new()
                {
                    Id = node.Id,
                    Label = node.Label,
                    IsExpanded = node.IsExpanded,
                    Data = node.Data
                };
                current.AddChild(child);
                current = child;
            }
            return true;
        });

        AssignNodeIds(root);

        return root;
    }
}
