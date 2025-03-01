using Cadmus.Core;
using Cadmus.Export.Renderers;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using Fusi.Tools.Text;
using Proteus.Core.Text;
using System;
using System.Linq;
using System.Xml.Linq;

namespace Cadmus.Export.ML.Renderers;

/// <summary>
/// Standoff TEI text tree renderer.
/// <para>Tag: <c>it.vedph.text-tree-renderer.tei-off-linear</c>.</para>
/// </summary>
[Tag("it.vedph.text-tree-renderer.tei-off-linear")]
public sealed class TeiOffLinearTextTreeRenderer : TextTreeRenderer,
    ITextTreeRenderer,
    IConfigurable<XmlTextTreeRendererOptions>
{
    private int _group;
    private string? _pendingGroupId;

    /// <summary>
    /// The name of the metadata placeholder for the item's ordinal number
    /// (1-N). This is set externally when repeatedly using this renderer
    /// for multiple items.
    /// </summary>
    public const string M_ITEM_NR = "item-nr";
    /// <summary>
    /// The name of the metadata placeholder for each block's target ID.
    /// Each target ID is built with item number + layer ID + block number,
    /// all separated by underscore and prefixed by an initial single <c>b</c>
    /// (e.g. <c>b1_2_3</c>).
    /// </summary>
    public const string M_TARGET_ID = "target-id";
    /// <summary>
    /// The name of the metadata placeholder for row's y number (1-N).
    /// </summary>
    public const string M_ROW_Y = "y";
    /// <summary>
    /// The name of the metadata placeholder for block's ID.
    /// </summary>
    public const string M_BLOCK_ID = "b";

    private XmlTextTreeRendererOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeiOffLinearTextTreeRenderer"/>
    /// class.
    /// </summary>
    public TeiOffLinearTextTreeRenderer()
    {
        _options = new XmlTextTreeRendererOptions();
    }

    /// <summary>
    /// Configures this renderer with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    public void Configure(XmlTextTreeRendererOptions options)
    {
        _options = options ?? new XmlTextTreeRendererOptions();
    }

    /// <summary>
    /// Resets the state of this renderer. This is called once before
    /// starting the rendering process.
    /// </summary>
    /// <param name="context">The context.</param>
    public override void Reset(IRendererContext context)
    {
        _group = 0;
        _pendingGroupId = null;
    }

    /// <summary>
    /// Called when items group has changed.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="prevGroupId">The previous group identifier.</param>
    /// <param name="context">The context.</param>
    public override void OnGroupChanged(IItem item, string? prevGroupId,
        IRendererContext context)
    {
        _group++;
        _pendingGroupId = item.GroupId;
    }

    /// <summary>
    /// Renders the specified tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>Rendition.</returns>
    /// <exception cref="ArgumentNullException">tree or context</exception>
    protected override string DoRender(TreeNode<TextSpanPayload> tree,
        IRendererContext context)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(context);

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

        // create root element like div @n="ITEM_NR
        XElement root = new(rootName);
        if (context.Data.TryGetValue(M_ITEM_NR, out object? nr))
            root.SetAttributeValue("n", nr);

        // create block element like p
        int y = 1;
        XElement block = new(blockName,
            new XAttribute("source", "$" + context.Item!.Id),
            new XAttribute("n", 1));
        root.Add(block);

        // traverse nodes and build the XML (each node corresponds to a fragment)
        tree.Traverse(node =>
        {
            if (node.Data?.Text != null)
            {
                if (node.Data.Range?.FragmentIds?.Count > 0)
                {
                    int id = context.MapSourceId("seg",
                        $"{context.Item!.Id}/{node.Id}");

                    XElement seg = new(NamespaceOptions.TEI + "seg",
                        new XAttribute(NamespaceOptions.XML + "id", $"seg{id}"),
                        node.Data.Text);

                    block.Add(seg);
                }
                else
                {
                    block.Add(node.Data.Text);
                }
            }

            // open a new block if needed
            if (node.Data?.IsBeforeEol == true)
            {
                block = new XElement(blockName,
                    new XAttribute("source", "$" + context.Item.Id),
                    new XAttribute("n", ++y));
                root.Add(block);
            }

            return true;
        });

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
        if (_pendingGroupId != null)
        {
            if (_group > 0 && !string.IsNullOrEmpty(_options.GroupTailTemplate))
            {
                xml = TextTemplate.FillTemplate(
                    _options.GroupTailTemplate, context.Data) + xml;
            }
            if (!string.IsNullOrEmpty(_options.GroupHeadTemplate))
            {
                xml = TextTemplate.FillTemplate(
                    _options.GroupHeadTemplate, context.Data) + xml;
            }
        }

        return xml;
    }
}
