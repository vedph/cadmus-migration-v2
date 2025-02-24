using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using Proteus.Text.Xml;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Cadmus.Export.ML;

/// <summary>
/// Linear text tree with single apparatus layer renderer.
/// </summary>
/// <seealso cref="ITextTreeRenderer" />
public sealed class AppLinearTextTreeRenderer : TextTreeRenderer,
    ITextTreeRenderer,
    IConfigurable<AppLinearTextTreeRendererOptions>
{
    /// <summary>
    /// The context block type data key to retrieve it from the context data.
    /// </summary>
    public const string CONTEXT_BLOCK_TYPE = "block-type";

    private AppLinearTextTreeRendererOptions _options = new();

    /// <summary>
    /// Configures this renderer with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    public void Configure(AppLinearTextTreeRendererOptions options)
    {
        _options = options ?? new AppLinearTextTreeRendererOptions();
    }

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="tree">The root node of the text tree.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>Rendered output.</returns>
    /// <exception cref="ArgumentNullException">tree</exception>
    protected override string DoRender(TreeNode<TextSpanPayload> tree,
        IRendererContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(tree);

        // get the root element name
        XName rootName = _options.ResolvePrefixedName(_options.RootElement);

        // get the block name
        string blockType = "default";
        if (context != null &&
            context.Data.TryGetValue(CONTEXT_BLOCK_TYPE, out object? value))
        {
            blockType = value as string ?? "default";
        }

        XName blockName = _options.ResolvePrefixedName(
            _options.DefaultNsPrefix + ":" + _options.BlockElements[blockType]);

        // calculate fragment ID prefix
        string prefix = TextSpanPayload.GetFragmentPrefixFor(
            new TokenTextLayerPart<ApparatusLayerFragment>(),
            new ApparatusLayerFragment());

        // create root element
        XElement root = new(rootName);
        XElement block = new(blockName);
        root.Add(block);

        // traverse nodes and build the XML
        tree.Traverse(node =>
        {
            // TODO

            // open a new block if needed
            if (node.Data?.IsBeforeEol == true)
            {
                block = new XElement(blockName);
                root.Add(block);
            }
            return true;
        });

        return root.ToString(_options.Indented
            ? SaveOptions.OmitDuplicateNamespaces
            : SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting);
    }
}

/// <summary>
/// Options for <see cref="AppLinearTextTreeRenderer"/>.
/// </summary>
/// <seealso cref="XmlTextFilterOptions" />
public class AppLinearTextTreeRendererOptions : XmlTextFilterOptions
{
    /// <summary>
    /// Gets or sets the name of the root element. The default is <c>div</c>.
    /// </summary>
    public string RootElement { get; set; }

    /// <summary>
    /// Gets or sets the block element name(s). The default name is "p" under
    /// a <c>default</c> key, other names can be specified for conditional
    /// element names (e.g. when dealing with poetry rather than prose).
    /// If you need to specify a namespaced name, use the format "prefix:name"
    /// and define the prefix in the <see cref="XmlTextFilterOptions.Namespaces"/>
    /// property.
    /// </summary>
    public IDictionary<string, string> BlockElements { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the XML output should be
    /// indented. This can be useful for diagnostic purposes.
    /// </summary>
    public bool Indented { get; set; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="AppLinearTextTreeRendererOptions"/> class.
    /// </summary>
    public AppLinearTextTreeRendererOptions()
    {
        RootElement = "div";
        BlockElements = new Dictionary<string, string>
        {
            ["default"] = "p"
        };
    }
}
