using Cadmus.Core;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using System;
using System.Globalization;
using System.Linq;

namespace Cadmus.Export.Filters;

/// <summary>
/// A text tree filter which uses the apparatus part of the item (with
/// <see cref="ApparatusLayerFragment"/>s), if any, to modify text and features
/// of a linear tree.
/// <para>Tag: <c>text-tree-filter.apparatus-linear</c>.</para>
/// </summary>
/// <remarks>This filter can modify the node's payload text, and add features
/// to it. Each feature has as source the fragment ID plus a suffix like
/// <c>.INDEX</c> where INDEX is the index of the fragment's entry
/// which generated it.</remarks>
/// <seealso cref="ITextTreeFilter" />
[Tag("text-tree-filter.apparatus-linear")]
public sealed class ApparatusLinearTextTreeFilter : ITextTreeFilter
{
    /// <summary>
    /// The name of the feature for the apparatus variant.
    /// </summary>
    public const string F_APP_VARIANT = "app-variant";
    /// <summary>
    /// The name of the feature for the apparatus note.
    /// </summary>
    public const string F_APP_NOTE = "app-note";
    /// <summary>
    /// The name of the feature for an apparatus entry witness.
    /// </summary>
    public const string F_APP_WITNESS = "app-witness";
    /// <summary>
    /// The name of the feature for an apparatus entry witness note.
    /// </summary>
    public const string F_APP_WITNESS_NOTE = "app-witness.note";
    /// <summary>
    /// The name if the feature for an apparatus entry author.
    /// </summary>
    public const string F_APP_AUTHOR = "app-author";
    /// <summary>
    /// The name of the feature for an apparatus entry author note.
    /// </summary>
    public const string F_APP_AUTHOR_NOTE = "app-author.note";

    private static void AddWitnessesOrAuthors(ApparatusEntry entry,
        TreeNode<TextSpanPayload> node, string source)
    {
        foreach (AnnotatedValue wit in entry.Witnesses)
        {
            node.Data!.Features.Add(new TextSpanFeature(
                F_APP_WITNESS, wit.Value!, source));
            if (!string.IsNullOrEmpty(wit.Note))
            {
                node.Data.Features.Add(new TextSpanFeature(
                    F_APP_WITNESS_NOTE, wit.Note, source));
            }
        }

        foreach (LocAnnotatedValue author in entry.Authors)
        {
            node.Data!.Features.Add(new TextSpanFeature(
                F_APP_AUTHOR, author.Value!, source));
            if (!string.IsNullOrEmpty(author.Note))
            {
                node.Data.Features.Add(new TextSpanFeature(
                    F_APP_AUTHOR_NOTE, author.Note, source));
            }
        }
    }

    private static void FeaturizeApparatus(TreeNode<TextSpanPayload> node,
        TokenTextLayerPart<ApparatusLayerFragment> part)
    {
        foreach (string id in node.Data!.Range.FragmentIds)
        {
            // get the source fragment
            int i = int.Parse(id[(id.LastIndexOf('@') + 1)..],
                CultureInfo.InvariantCulture);
            ApparatusLayerFragment fr = part.Fragments[i];

            int entryIndex = 0;
            foreach (ApparatusEntry entry in fr.Entries)
            {
                switch (entry.Type)
                {
                    case ApparatusEntryType.AdditionBefore:
                        // if accepted, prepend to text and add original text
                        // as variant; otherwise, add as variant
                        if (entry.IsAccepted)
                        {
                            node.Data.Features.Add(new TextSpanFeature(
                                F_APP_VARIANT, node.Data.Text!, $"{id}.{entryIndex}"));
                            node.Data.Text = entry.Value + node.Data.Text;
                        }
                        else
                        {
                            node.Data.Features.Add(new TextSpanFeature(
                                F_APP_VARIANT, entry.Value ?? "", $"{id}.{entryIndex}"));
                        }
                        break;

                    case ApparatusEntryType.AdditionAfter:
                        // if accepted, append to text and add original text
                        // as variant; otherwise, add as variant
                        if (entry.IsAccepted)
                        {
                            node.Data.Features.Add(new TextSpanFeature(
                                F_APP_VARIANT, node.Data.Text!, $"{id}.{entryIndex}"));
                            node.Data.Text += entry.Value;
                        }
                        else
                        {
                            node.Data.Features.Add(new TextSpanFeature(
                                F_APP_VARIANT, entry.Value ?? "", $"{id}.{entryIndex}"));
                        }
                        break;

                    case ApparatusEntryType.Replacement:
                        // if accepted, replace text and add original text as
                        // variant; otherwise, add as variant
                        if (entry.IsAccepted)
                        {
                            node.Data.Features.Add(new TextSpanFeature(
                                F_APP_VARIANT, node.Data.Text!, $"{id}.{entryIndex}"));
                            node.Data.Text = entry.Value;
                        }
                        else
                        {
                            node.Data.Features.Add(new TextSpanFeature(
                                F_APP_VARIANT, entry.Value ?? "", $"{id}.{entryIndex}"));
                        }
                        break;
                }

                // add witnesses and authors
                AddWitnessesOrAuthors(entry, node, $"{id}.{entryIndex}");

                // add note if any
                if (!string.IsNullOrEmpty(entry.Note))
                {
                    node.Data.Features.Add(new TextSpanFeature(
                        F_APP_NOTE, entry.Note, $"{id}.{entryIndex}"));
                }

                entryIndex++;
            }
        }
    }

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
            TokenTextLayerPart<ApparatusLayerFragment> part)
        {
            return tree;
        }

        string prefix = $"{part.TypeId}:{part.RoleId}_";

        tree.Traverse(node =>
        {
            if (node.Data?.Range?.FragmentIds?.Any(id => id.StartsWith(prefix))
                == true)
            {
                FeaturizeApparatus(node, part);
            }
            return true;
        });

        return tree;
    }
}
