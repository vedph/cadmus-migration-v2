using Cadmus.Export.Filters;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using MongoDB.Driver;
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

    /// <summary>
    /// The key used in the rendering context to keep track of unique identifiers
    /// for segments like <c>lem</c> and <c>rdg</c>.
    /// </summary>
    public const string CONTEXT_SEG_IDKEY = "seg";

    private AppLinearTextTreeRendererOptions _options = new();

    /// <summary>
    /// Configures this renderer with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    public void Configure(AppLinearTextTreeRendererOptions options)
    {
        _options = options ?? new AppLinearTextTreeRendererOptions();
    }

    private void AddWitDetail(string segName, string? segId, string detail,
        XElement seg, IRendererContext context)
    {
        // witDetail
        XElement witDetail = new(_options.ResolvePrefixedName("tei:witDetail"),
            detail);
        seg.Add(witDetail);

        // @target=segID
        int targetId = GetNextIdFor(CONTEXT_SEG_IDKEY, context);
        seg.SetAttributeValue(_options.ResolvePrefixedName("xml:id"),
            $"seg{targetId}");
        witDetail.SetAttributeValue("target", $"#seg{targetId}");

        // @wit or @resp
        witDetail.SetAttributeValue(segName, $"#{segId}");
    }

    private void EnrichSegment(IList<TextSpanFeature> features, XElement seg,
        IRendererContext context)
    {
        string? prevSeg = null;
        StringBuilder wit = new();
        StringBuilder resp = new();

        foreach (TextSpanFeature feature in features)
        {
            switch (feature.Name)
            {
                case AppLinearTextTreeFilter.F_APP_E_WITNESS:
                    if (wit.Length > 0) wit.Append(' ');
                    wit.Append('#').Append(feature.Value);
                    prevSeg = feature.Value;
                    break;

                case AppLinearTextTreeFilter.F_APP_E_WITNESS_NOTE:
                    AddWitDetail("wit", prevSeg, feature.Value, seg,
                        context);
                    break;

                case AppLinearTextTreeFilter.F_APP_E_AUTHOR:
                    if (resp.Length > 0) resp.Append(' ');
                    resp.Append('#').Append(feature.Value);
                    prevSeg = feature.Value;
                    break;

                case AppLinearTextTreeFilter.F_APP_E_AUTHOR_NOTE:
                    AddWitDetail("resp", prevSeg, feature.Value, seg,
                        context);
                    break;
            }
        }

        if (wit.Length > 0)
            seg.SetAttributeValue("wit", wit.ToString());
        if (resp.Length > 0)
            seg.SetAttributeValue("resp", resp.ToString());
    }

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="tree">The root node of the text tree.</param>
    /// <param name="context">The renderer context.</param>
    /// <returns>Rendered output.</returns>
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
        if (context.Data.TryGetValue(CONTEXT_BLOCK_TYPE, out object? value))
        {
            blockType = value as string ?? "default";
        }

        XName blockName = _options.ResolvePrefixedName(
            _options.BlockElements[blockType]);

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
                    // get all the features in the entry set
                    List<TextSpanFeature> features =
                        node.Data.FeatureSets[entryKey].Features;

                    // if there a variant, it's a rdg, else it's a lem
                    XElement seg = (features.Any(f => f.Name ==
                        AppLinearTextTreeFilter.F_APP_E_VARIANT))
                        ? new XElement(_options.ResolvePrefixedName("tei:rdg"))
                        {
                            Value = features.First(f => f.Name ==
                                AppLinearTextTreeFilter.F_APP_E_VARIANT).Value
                        }
                        : new XElement(_options.ResolvePrefixedName("tei:lem"))
                        {
                            Value = node.Data.Text!
                        };

                    EnrichSegment(features, seg, context);
                    app.Add(seg);

                    // if there is a note, add a note child element
                    TextSpanFeature? noteFeature = features.FirstOrDefault(
                        f => f.Name == AppLinearTextTreeFilter.F_APP_E_NOTE);
                    if (noteFeature != null)
                    {
                        XElement note = new(
                            _options.ResolvePrefixedName("tei:note"),
                            noteFeature.Value);
                        app.Add(note);
                    }
                } // entry
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

        if (_options.IsRootIncluded)
        {
            return root.ToString(_options.IsIndented
                ? SaveOptions.OmitDuplicateNamespaces
                : SaveOptions.OmitDuplicateNamespaces |
                  SaveOptions.DisableFormatting);
        }

        return string.Concat(root.Nodes().Select(
            node => node.ToString(_options.IsIndented
            ? SaveOptions.OmitDuplicateNamespaces
            : SaveOptions.OmitDuplicateNamespaces |
                SaveOptions.DisableFormatting)));
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
    /// This is usually not rendered in output, but it is used as the root of
    /// the XML fragment built by the renderer.
    /// </summary>
    public string RootElement { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="RootElement"/>
    /// should be included in the output. The default is <c>false</c>.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is root included; otherwise, <c>false</c>.
    /// </value>
    public bool IsRootIncluded { get; set; }

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
    public bool IsIndented { get; set; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="AppLinearTextTreeRendererOptions"/> class.
    /// </summary>
    public AppLinearTextTreeRendererOptions()
    {
        DefaultNsPrefix = "tei";
        RootElement = "tei:div";
        BlockElements = new Dictionary<string, string>
        {
            ["default"] = "tei:p"
        };
    }
}
