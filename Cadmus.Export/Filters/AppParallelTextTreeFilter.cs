using Cadmus.Core;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadmus.Export.Filters;

/// <summary>
/// A filter whose task is to add branching at each point where the text of
/// each source (witness or author) diverges from the base text, limiting the
/// children of each node to maximum two. To this end, whenever branching
/// occurs the filter inserts a new blank node which forks into two branches.
/// <para>Tag: <c>it.vedph.text-tree-filter.app-parallel</c>.</para>
/// </summary>
/// <remarks>This apparatus layer merger tree filter collects variants from
/// apparatus fragments, merging into a single tree each version of the text
/// as deduced from the apparatus (of course we are assuming that such
/// information is present).</remarks>
/// <seealso cref="ITextTreeFilter" />
[Tag("it.vedph.text-tree-filter.app-parallel")]
public sealed class AppParallelTextTreeFilter : ITextTreeFilter,
    IConfigurable<AppParallelTextTreeFilterOptions>
{
    /// <summary>
    /// The feature name for a text version tag.
    /// </summary>
    public const string FN_VERSION_TAG = "tag";

    private readonly TreeNodeVersionMerger<TextSpan> _merger = new(
        new SpanTreeNodePayloadTagger());
    private AppParallelTextTreeFilterOptions? _options;

    /// <summary>
    /// Configures this filter with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    public void Configure(AppParallelTextTreeFilterOptions options)
    {
        _options = options;
    }

    private string? ReplaceSource(string source, bool author)
    {
        if (_options == null) return source;

        IDictionary<string, string>? reps = author
            ? _options.AuthReplacements
            : _options.WitReplacements;
        if (reps == null) return source;

        return reps.TryGetValue(source, out string? value) ? value : source;
    }

    private HashSet<Tuple<bool,string>> CollectSources(TreeNode<TextSpan> tree,
        TokenTextLayerPart<ApparatusLayerFragment> part, string prefix)
    {
        HashSet<Tuple<bool, string>> sources = [];

        tree.Traverse(node =>
        {
            if (node.Data != null)
            {
                // find apparatus fragment starting with prefix
                string? frId = node.Data.Range?.FragmentIds?.Find(
                    id => id.StartsWith(prefix));

                if (frId != null)
                {
                    // get the fragment
                    ApparatusLayerFragment fragment = part.Fragments[
                        TextSpan.GetFragmentIndex(frId)];

                    // read all the sources from the fragment
                    foreach (ApparatusEntry entry in fragment.Entries)
                    {
                        // witnesses
                        foreach (string wit in entry.Witnesses
                            .Where(av => av.Value != null)
                            .Select(av => av.Value!))
                        {
                            string? filteredWit = ReplaceSource(wit, false);
                            if (!string.IsNullOrEmpty(filteredWit))
                                sources.Add(Tuple.Create(false, filteredWit));
                        }

                        // authors
                        foreach (string auth in entry.Authors
                            .Where(av => av.Value != null)
                            .Select(av => av.Value!))
                        {
                            string? filteredAuth = ReplaceSource(auth, true);
                            if (!string.IsNullOrEmpty(filteredAuth))
                                sources.Add(Tuple.Create(true, filteredAuth));
                        }
                    }
                }
            }

            return true;
        });

        return sources;
    }

    private ApparatusEntry? FindEntryBySource(ApparatusLayerFragment fragment,
        string source, bool author)
    {
        return fragment.Entries.FirstOrDefault(e =>
        {
            // authors
            if (author)
            {
                foreach (string auth in e.Authors
                    .Where(av => av.Value != null)
                    .Select(av => av.Value!))
                {
                    string? filteredAuth = ReplaceSource(auth, true);
                    if (filteredAuth == source) return true;
                }
            }
            // witnesses
            else
            {
                foreach (string wit in e.Witnesses
                    .Where(av => av.Value != null)
                    .Select(av => av.Value!))
                    {
                        string? filteredWit = ReplaceSource(wit, false);
                        if (filteredWit == source) return true;
                    }
            }
            return false;
        });
    }

    private TreeNode<TextSpan> BuildVersionTree(TreeNode<TextSpan> tree,
        TokenTextLayerPart<ApparatusLayerFragment> part, string prefix,
        string tag, bool author)
    {
        TreeNode<TextSpan> root = new();
        TreeNode<TextSpan> current = root;

        string prefixedTag = $"{(author ? "a:" : "w:")}{tag}";

        tree.Traverse(node =>
        {
            string? frId;
            TreeNode<TextSpan>? child = null;

            if (node.Data != null &&
                (frId = node.Data.Range?.FragmentIds?.Find(
                    id => id.StartsWith(prefix))) != null)
            {
                ApparatusLayerFragment fragment = part.Fragments[
                    TextSpan.GetFragmentIndex(frId)];

                ApparatusEntry? entry = FindEntryBySource(fragment, tag, author);
                if (entry != null)
                {
                    string text = entry.Type == ApparatusEntryType.Note
                        ? node.Data.Text! : entry.Value ?? "";

                    child = new(new TextSpan
                    {
                        IsBeforeEol = node.Data.IsBeforeEol,
                        Text = text,
                    })
                    {
                        Id = node.Id,
                        Label = text
                    };
                    child.Data!.AddFeature(FN_VERSION_TAG, prefixedTag);
                    current.AddChild(child);
                }
            }

            if (child == null)
            {
                child = node.Clone(false, false);
                current.AddChild(child);
            }

            current = child;

            return true;
        });

        return root;
    }

    /// <summary>
    /// Applies this filter to the specified tree, generating a new tree.
    /// </summary>
    /// <param name="tree">The tree's root node.</param>
    /// <param name="item">The item being rendered.</param>
    /// <returns>The root node of the new tree.</returns>
    /// <exception cref="ArgumentNullException">tree or item</exception>
    public TreeNode<TextSpan> Apply(TreeNode<TextSpan> tree, IItem item)
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

        // collect all the unique sources identifiers from witnesses/authors
        // (item1=true for authors, false for witnesses)
        HashSet<Tuple<bool, string>> sources = CollectSources(tree, part, prefix);
        Dictionary<Tuple<bool, string>, TreeNode<TextSpan>> versions = [];

        // base text version has empty tag
        versions[Tuple.Create(false, "")] = tree.Clone();

        // collect the trees representing the other versions
        foreach (var source in sources)
        {
            versions[source] = BuildVersionTree(tree, part, prefix,
                source.Item2, source.Item1);
        }

        // merge each version into a new tree
        List<string> prefixedTags = [.. sources
            .Select(t => $"{(t.Item1 ? "a:" : "w:")}{t.Item2}")];

        return _merger.Merge(prefixedTags, prefixedTag =>
        {
            bool author = prefixedTag.StartsWith("a:");
            return versions[Tuple.Create(author, prefixedTag[2..])];
        }, true);
    }
}

/// <summary>
/// Options for <see cref="AppParallelTextTreeFilter"/>.
/// </summary>
public class AppParallelTextTreeFilterOptions
{
    /// <summary>
    /// Gets or sets the witness identifiers replacements. This can be used
    /// to filter witnesses, e.g. treating O1 as O. If the replacement value
    /// is empty, the witness will be ignored.
    /// </summary>
    public IDictionary<string, string>? WitReplacements { get; set; }

    /// <summary>
    /// Gets or sets the authors replacements. This can be used to filter
    /// authors. If the replacement value is empty, the author will be ignored.
    /// /// </summary>
    public IDictionary<string, string>? AuthReplacements { get; set; }
}
