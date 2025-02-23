using Cadmus.Core;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using System;
using System.Linq;

namespace Cadmus.Export.Filters;

/// <summary>
/// A text tree filter which uses the apparatus part of the item (with
/// <see cref="ApparatusLayerFragment"/>s), if any, to modify text and features
/// of a linear tree.
/// </summary>
/// <seealso cref="ITextTreeFilter" />
[Tag("text-tree-filter.apparatus-linear")]
public sealed class ApparatusLinearTextTreeFilter : ITextTreeFilter
{
    /// <summary>
    /// Applies this filter to the specified tree, generating a new tree.
    /// </summary>
    /// <param name="tree">The tree's root node.</param>
    /// <param name="item">The item being rendered.</param>
    /// <returns>
    /// The root node of the new tree.
    /// </returns>
    /// <exception cref="ArgumentNullException">tree or item</exception>
    public TreeNode<TextSpanPayload> Apply(TreeNode<TextSpanPayload> tree,
        IItem item)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(item);

        // nope if no apparatus part
        if (item.Parts.FirstOrDefault(p =>
            p is TokenTextLayerPart<ApparatusLayerFragment>) is not
            TokenTextLayerPart<ApparatusLayerFragment> appPart)
        {
            return tree;
        }

        string prefix = $"{appPart.TypeId}:{appPart.RoleId}_";

        foreach (ApparatusLayerFragment fr in appPart.Fragments)
        {
            foreach (ApparatusEntry entry in fr.Entries)
            {
                
            }
        }
        throw new NotImplementedException();
    }
}
