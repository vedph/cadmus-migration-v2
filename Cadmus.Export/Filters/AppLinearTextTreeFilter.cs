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
/// <para>Tag: <c>it.vedph.text-tree-filter.apparatus-linear</c>.</para>
/// </summary>
/// <seealso cref="ITextTreeFilter" />
[Tag("it.vedph.text-tree-filter.apparatus-linear")]
public sealed class AppLinearTextTreeFilter : ITextTreeFilter
{
    /// <summary>
    /// The name of the feature for apparatus tags.
    /// </summary>
    public const string F_APP_TAG = "app-tag";

    /// <summary>
    /// The name of the feature for apparatus entries tags.
    /// </summary>
    public const string F_APP_E_TAG = "app.e.tag";

    /// <summary>
    /// The name of the feature for an entry note.
    /// </summary>
    public const string F_APP_E_NOTE = "app.e.note";

    /// <summary>
    /// The name of the feature for the apparatus variant. This is the entry
    /// value when the entry is not of type note.
    /// </summary>
    public const string F_APP_E_VARIANT = "app.e.variant";

    /// <summary>
    /// The name of the feature for an apparatus entry witness.
    /// </summary>
    public const string F_APP_E_WITNESS = "app.e.witness";

    /// <summary>
    /// The name of the feature for an apparatus entry witness note.
    /// </summary>
    public const string F_APP_E_WITNESS_NOTE = "app.e.witness.note";

    /// <summary>
    /// The name of the feature for an apparatus entry author.
    /// </summary>
    public const string F_APP_E_AUTHOR = "app.e.author";

    /// <summary>
    /// The name of the feature for an apparatus entry author note.
    /// </summary>
    public const string F_APP_E_AUTHOR_NOTE = "app.e.author.note";

    private static void AddWitnessesOrAuthors(ApparatusEntry entry,
        TextSpanPayload payload, string key, string source)
    {
        foreach (AnnotatedValue wit in entry.Witnesses)
        {
            payload.AddFeatureToSet(
                key,
                new TextSpanFeature(F_APP_E_WITNESS, wit.Value!),
                source);

            if (!string.IsNullOrEmpty(wit.Note))
            {
                payload.AddFeatureToSet(
                    key,
                    new TextSpanFeature(F_APP_E_WITNESS_NOTE, wit.Note),
                    source);
            }
        }

        foreach (LocAnnotatedValue author in entry.Authors)
        {
            payload.AddFeatureToSet(
                key,
                new TextSpanFeature(F_APP_E_AUTHOR, author.Value!),
                source);

            if (!string.IsNullOrEmpty(author.Note))
            {
                payload.AddFeatureToSet(
                    key,
                    new TextSpanFeature(F_APP_E_AUTHOR_NOTE, author.Note),
                    source);
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

            // process its entries
            int entryIndex = 0;

            foreach (ApparatusEntry entry in fr.Entries)
            {
                string setKey = $"e{entryIndex:000}";
                string source = $"{id}.{entryIndex}";

                switch (entry.Type)
                {
                    case ApparatusEntryType.AdditionBefore:
                        // if not accepted add a variant
                        if (!entry.IsAccepted)
                        {
                            node.Data.AddFeatureToSet(
                                setKey,
                                new TextSpanFeature(
                                    F_APP_E_VARIANT,
                                    entry.Value + node.Data.Text),
                                source);
                        }
                        break;

                    case ApparatusEntryType.AdditionAfter:
                        // if not accepted add a variant
                        if (!entry.IsAccepted)
                        {
                            node.Data.AddFeatureToSet(
                                setKey,
                                new TextSpanFeature(
                                    F_APP_E_VARIANT,
                                    node.Data.Text + entry.Value),
                                source);
                        }
                        break;

                    case ApparatusEntryType.Replacement:
                        // if not accepted add a variant unless equal to the base
                        // text (in the case where users added replacements with
                        // self rather than note as apparatus entries)
                        if (!entry.IsAccepted && node.Data.Text != entry.Value)
                        {
                            node.Data.AddFeatureToSet(
                                setKey,
                                new TextSpanFeature(
                                    F_APP_E_VARIANT,
                                    entry.Value ?? ""),
                                source);
                        }
                        break;
                }

                // add witnesses and authors with their notes
                if (entry.Witnesses.Count > 0 || entry.Authors.Count > 0)
                    AddWitnessesOrAuthors(entry, node.Data, setKey, source);

                // add entry note
                if (!string.IsNullOrEmpty(entry.Note))
                {
                    node.Data.AddFeatureToSet(setKey,
                        new TextSpanFeature(F_APP_E_NOTE, entry.Note),
                        $"{id}.{entryIndex}");
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

        string prefix = $"{part.TypeId}:{part.RoleId}@";

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
