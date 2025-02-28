using Cadmus.Core;
using Cadmus.Export.Renderers;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using Fusi.Tools.Text;
using MongoDB.Driver;
using Proteus.Text.Xml;
using System;
using System.Linq;
using System.Xml.Linq;

namespace Cadmus.Export.ML.Renderers;

/// <summary>
/// TEI linear text tree with single apparatus layer renderer.
/// <para>Tag: <c>it.vedph.text-tree-renderer.tei-app-linear</c>.</para>
/// </summary>
/// <seealso cref="ITextTreeRenderer" />
[Tag("it.vedph.text-tree-renderer.tei-app-linear")]
public sealed class TeiAppLinearTextTreeRenderer : TextTreeRenderer,
    ITextTreeRenderer,
    IConfigurable<AppLinearTextTreeRendererOptions>
{
    private TeiHelper _tei;
    private int _group;
    private string? _pendingGroupId;

    private AppLinearTextTreeRendererOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeiAppLinearTextTreeRenderer"/>
    /// class.
    /// </summary>
    public TeiAppLinearTextTreeRenderer()
    {
        _options = new();
        _tei = new TeiHelper(_options);
    }

    /// <summary>
    /// Configures this renderer with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    public void Configure(AppLinearTextTreeRendererOptions options)
    {
        _options = options ?? new AppLinearTextTreeRendererOptions();
        _tei = new TeiHelper(_options);
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
            ? TextSpanPayload.GetFragmentPrefixFor(layerPart) : null;

        // create root element
        XElement root = new(rootName);
        XElement block = new(blockName);
        root.Add(block);

        // traverse nodes and build the XML (each node corresponds to a fragment)
        tree.Traverse(node =>
        {
            string? frId = prefix != null?
                node.Data?.GetLinkedFragmentId(prefix) : null;
            if (frId != null)
            {
                // get the index of the fragment linked to this node
                int frIndex = TextSpanPayload.GetFragmentIndex(frId);

                // app
                XElement app = _tei.BuildAppElement(textPart.Id,
                    layerPart!.Fragments[frIndex], frIndex, false,
                    _options.ZeroVariantType)!;
                block.Add(app);
            }
            else
            {
                if (!string.IsNullOrEmpty(node.Data?.Text))
                    block.Add(node.Data.Text);
            }

            // open a new block if needed
            if (node.Data?.IsBeforeEol == true)
            {
                block = new XElement(blockName);
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

    /// <summary>
    /// Renders the tail of the output. This is called by the item composer
    /// once when ending the rendering process and can be used to output
    /// specific content at the document's end.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>Tail content.</returns>
    public override string RenderTail(IRendererContext context)
    {
        // close a group if any
        if (_group > 0 && !string.IsNullOrEmpty(_options.GroupTailTemplate))
        {
            return TextTemplate.FillTemplate(
                _options.GroupTailTemplate, context.Data);
        }
        return "";
    }
}

/// <summary>
/// Options for <see cref="TeiAppLinearTextTreeRenderer"/>.
/// </summary>
/// <seealso cref="XmlTextFilterOptions" />
public class AppLinearTextTreeRendererOptions : XmlTextTreeRendererOptions
{
    /// <summary>
    /// Gets or sets the value for the type attribute to add to <c>rdg</c>
    /// elements for zero-variants, i.e. variants with no text meaning an
    /// omission. If null, no attribute will be added. The default is null.
    /// </summary>
    public string? ZeroVariantType { get; set; }
}
