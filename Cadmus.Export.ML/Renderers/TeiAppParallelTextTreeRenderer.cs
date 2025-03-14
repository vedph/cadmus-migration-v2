using Cadmus.Core;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using Proteus.Core.Text;
using Proteus.Text.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Cadmus.Export.ML.Renderers;

/// <summary>
/// TEI parallel segmentation tree with single apparatus layer renderer.
/// <para>Tag: <c>it.vedph.text-tree-renderer.tei-app-parallel</c>.</para>
/// </summary>
[Tag("it.vedph.text-tree-renderer.tei-app-parallel")]
public sealed class TeiAppParallelTextTreeRenderer : GroupTextTreeRenderer,
    ITextTreeRenderer,
    IConfigurable<TeiAppParallelTextTreeRendererOptions>
{
    private TeiHelper _tei;
    private TeiAppParallelTextTreeRendererOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeiAppParallelTextTreeRenderer"/>
    /// class.
    /// </summary>
    public TeiAppParallelTextTreeRenderer()
    {
        _options = new();
        _tei = new TeiHelper(_options);
    }

    /// <summary>
    /// Configures this renderer with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    public void Configure(TeiAppParallelTextTreeRendererOptions options)
    {
        _options = options ?? new TeiAppParallelTextTreeRendererOptions();
        _tei = new TeiHelper(_options);

        GroupHeadTemplate = _options.GroupHeadTemplate;
        GroupTailTemplate = _options.GroupTailTemplate;
    }

    private static void ProcessLevelNodes(List<TreeNode<TextSpan>> nodes,
        XElement targetElem)
    {
        if (nodes.Count == 0) return;

        // for single node we just output its text
        if (nodes.Count == 1)
        {
            targetElem.Add(nodes[0].Data!.Text);
        }
        // for multiple nodes, add as many child elements of type lem or rdg
        // using lem only when the node's text is original
        else
        {
            XElement app = new(NamespaceOptions.TEI + "app");
            targetElem.Add(app);

            // for each node, add an entry
            foreach (TreeNode<TextSpan> node in nodes)
            {
                // TODO apparatus attrs
                TextSpan span = node.Data!;
                XElement entryElem = span.Text == span.Range?.Text
                    ? new(NamespaceOptions.TEI + "lem", span.Text)
                    : new(NamespaceOptions.TEI + "rdg", span.Text);
                app.Add(entryElem);
            }
        }
    }

    /// <summary>
    /// Renders the specified tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>Rendition.</returns>
    /// <exception cref="ArgumentNullException">tree or context</exception>
    protected override string DoRender(TreeNode<TextSpan> tree,
        IRendererContext context)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(context);

        // configure the helper
        _tei.Configure(context, tree);

        // get the root element name
        XName rootName = _options.ResolvePrefixedName(_options.RootElement);

        // get the block name
        string blockType = "default";
        if (context.Data.TryGetValue(
            XmlTextTreeRendererOptions.CONTEXT_BLOCK_TYPE_KEY,
            out object? value))
        {
            blockType = value as string ?? "default";
        }

        XName blockName = _options.ResolvePrefixedName(
            _options.BlockElements[blockType]);

        // get text part
        IPart? textPart = context.GetTextPart();
        if (textPart == null) return "";    // should not happen

        // get apparatus layer part
        TokenTextLayerPart<ApparatusLayerFragment>? layerPart =
            context.Item!.Parts.FirstOrDefault(p =>
                p.TypeId == "it.vedph.token-text-layer" &&
                p.RoleId == "fr.it.vedph.apparatus")
            as TokenTextLayerPart<ApparatusLayerFragment>;

        // calculate the apparatus fragment ID prefix
        // (like "it.vedph.token-text-layer:fr.it.vedph.comment@")
        string? prefix = layerPart != null
            ? TextSpan.GetFragmentPrefixFor(layerPart) : null;

        // create root element
        XElement root = new(rootName);
        XElement block = new(blockName,
            _options.NoItemSource
                ? null
                : new XAttribute("source",
                    TeiItemComposer.ITEM_ID_PREFIX + context.Item.Id),
            new XAttribute("n", 1));
        root.Add(block);

        // traverse nodes and build the XML (each node corresponds to a fragment)
        int y = 2;
        List<TreeNode<TextSpan>> levelNodes = [];

        // traverse breadth-first so we can group nodes by their Y level
        tree.Traverse(node =>
        {
            // skip blank nodes (root)
            if (node.Data == null) return true;

            // while the node belongs to the current level, add it to the list
            // unless its text is a duplicate
            if (node.GetY() == y)
            {
                if (levelNodes.All(n => n.Data?.Text != node.Data?.Text))
                    levelNodes.Add(node);
            }
            // otherwise, process the current level nodes and start a new group
            else
            {
                ProcessLevelNodes(levelNodes, block);
                levelNodes.Clear();
                levelNodes.Add(node);
                y = node.GetY();
            }
            return true;
        }, true);

        // process the last group
        ProcessLevelNodes(levelNodes, block);

        string xml = _options.IsRootIncluded
            ? root.ToString(_options.IsIndented
                ? SaveOptions.OmitDuplicateNamespaces
                : SaveOptions.OmitDuplicateNamespaces |
                  SaveOptions.DisableFormatting)
            : string.Concat(root.Nodes().Select(
            node => node.ToString(_options.IsIndented
            ? SaveOptions.OmitDuplicateNamespaces
            : SaveOptions.OmitDuplicateNamespaces |
                SaveOptions.DisableFormatting)));

        // if there is a pending group ID:
        // - if there is a current group, prepend tail.
        // - prepend head.
        return WrapXml(xml, context);
    }
}

/// <summary>
/// Options for <see cref="TeiAppLinearTextTreeRenderer"/>.
/// </summary>
/// <seealso cref="XmlTextFilterOptions" />
public class TeiAppParallelTextTreeRendererOptions : XmlTextTreeRendererOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to omit item source in the
    /// output XML. Item source can be used either for diagnostic purposes
    /// or for resolving links in a filter. If you don't need them and you
    /// want a smaller XML you can set this to true.
    /// </summary>
    public bool NoItemSource { get; set; }
}
