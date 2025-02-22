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
    /// <returns>The root node of the new tree.</returns>
    public TreeNode<TextSpanPayload> Apply(TreeNode<TextSpanPayload> tree);
}
