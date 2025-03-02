using Cadmus.Core;
using Fusi.Tools.Data;

namespace Cadmus.Export;

/// <summary>
/// A filter to be applied to a text tree.
/// </summary>
public interface ITextTreeFilter
{
    /// <summary>
    /// Applies this filter to the specified tree, generating a new tree.
    /// </summary>
    /// <param name="tree">The tree's root node.</param>
    /// <param name="item">The item being rendered.</param>
    /// <returns>The root node of the new tree.</returns>
    public TreeNode<TextSpan> Apply(TreeNode<TextSpan> tree,
        IItem item);
}
