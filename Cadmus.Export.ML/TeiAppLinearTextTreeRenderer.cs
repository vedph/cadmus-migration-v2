using Cadmus.Export.Filters;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using Proteus.Text.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Cadmus.Export.ML;

/// <summary>
/// TEI linear text tree with single apparatus layer renderer.
/// <para>Tag: <c>text-tree-renderer.tei-app-linear</c>.</para>
/// </summary>
/// <seealso cref="ITextTreeRenderer" />
[Tag("text-tree-renderer.tei-app-linear")]
public sealed class TeiAppLinearTextTreeRenderer : TextTreeRenderer,
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

    private void EnrichSegment(IList<Tuple<FragmentFeatureSource,
        TextSpanFeature>> features, XElement seg)
    {
        // @wit: join all witnesses
        if (features.Any(f => f.Item2.Name ==
            AppLinearTextTreeFilter.F_APP_E_WITNESS))
        {
            string wit = string.Join(" ", features.Where(f =>
                f.Item2.Name == AppLinearTextTreeFilter.F_APP_E_WITNESS)
                .Select(f => "#" + f.Item2.Value));
            seg.SetAttributeValue("wit", wit);
        }

        // @resp: join all authors
        if (features.Any(f => f.Item2.Name ==
            AppLinearTextTreeFilter.F_APP_E_AUTHOR))
        {
            string resp = string.Join(" ", features.Where(f =>
                f.Item2.Name == AppLinearTextTreeFilter.F_APP_E_AUTHOR)
                .Select(f => "#" + f.Item2.Value));
            seg.SetAttributeValue("resp", resp);
        }

        // witnesses notes:
        // <witDetail @target=segID @wit/@resp
        // TODO
        
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

        // calculate the apparatus fragment ID prefix
        string prefix = TextSpanPayload.GetFragmentPrefixFor(
            new TokenTextLayerPart<ApparatusLayerFragment>(),
            new ApparatusLayerFragment());

        // create root element
        XElement root = new(rootName);
        XElement block = new(blockName);
        root.Add(block);

        // traverse nodes and build the XML (each node corresponds to a fragment)
        tree.Traverse(node =>
        {
            if (node.Data?.HasFeaturesFromFragment(prefix) == true)
            {
                // app
                XElement app = new(_options.ResolvePrefixedName("tei:app"));
                block.Add(app);

                // for each set (entry) in key order (e000, e001, ...)
                foreach (string entryKey in node.Data.FeatureSets.Keys.Order())
                {
                    List<TextSpanFeature> features =
                        node.Data.FeatureSets[entryKey].Features;
                }

                // TODO
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

        return root.ToString(_options.Indented
            ? SaveOptions.OmitDuplicateNamespaces
            : SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting);
    }
}

/// <summary>
/// Options for <see cref="TeiAppLinearTextTreeRenderer"/>.
/// </summary>
/// <seealso cref="XmlTextFilterOptions" />
public class AppLinearTextTreeRendererOptions : XmlTextFilterOptions
{
    /// <summary>
    /// Gets or sets the name of the root element. The default is <c>tei:div</c>.
    /// </summary>
    public string RootElement { get; set; }

    /// <summary>
    /// Gets or sets the block element name(s). The default name is "tei:p" under
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
        RootElement = "tei:div";
        BlockElements = new Dictionary<string, string>
        {
            ["default"] = "tei:p"
        };
    }
}
