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
        int y = 1;

        // stack to keep track of the current parent element during traversal
        Stack<XElement> elementStack = new();
        elementStack.Push(block);

        tree.Traverse(node =>
        {
            // skip root node
            if (node.Parent == null) return true;

            // get the current parent element
            XElement currentParent = elementStack.Peek();

            // if node has no data and has 2 children, create an app element
            if (node.Data == null && node.Children.Count == 2)
            {
                // create app element
                XElement appElement = new(NamespaceOptions.TEI + "app");
                currentParent.Add(appElement);

                // push this app element to the stack for its children
                elementStack.Push(appElement);

                // we'll pop this element after processing all its children
                // at this point we continue traversal, children will be
                // handled in their turn
                return true;
            }
            // for leaf nodes with text content
            else if (node.Data?.Text != null && node.Children.Count == 0)
            {
                // if parent is an app element, add a rdg element
                if (currentParent.Name.LocalName == "app")
                {
                    // TODO lem vs rdg
                    XElement rdgElement = new(NamespaceOptions.TEI + "rdg")
                    {
                        Value = node.Data.Text
                    };
                    currentParent.Add(rdgElement);
                }
                // otherwise add the text directly
                else
                {
                    currentParent.Add(new XText(node.Data.Text));
                }
            }

            // check if we need to pop the element stack
            // if this is the last child of its parent and its parent
            // is not the block
            if (node.Parent != null && node.Parent.Children.IndexOf(node) ==
                node.Parent.Children.Count - 1 && elementStack.Count > 1)
            {
                // pop the element if we're processing the last child
                elementStack.Pop();
            }

            // open a new block if needed
            if (node.Data?.IsBeforeEol == true)
            {
                block = new XElement(blockName,
                    _options.NoItemSource
                        ? null
                        : new XAttribute("source",
                            TeiItemComposer.ITEM_ID_PREFIX + context.Item.Id),
                    new XAttribute("n", ++y));
                root.Add(block);

                // reset the element stack with the new block
                elementStack.Clear();
                elementStack.Push(block);
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
