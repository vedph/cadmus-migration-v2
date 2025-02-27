﻿using Cadmus.Core;
using Cadmus.Export.Filters;
using Cadmus.Export.Renderers;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using Fusi.Tools.Text;
using MongoDB.Driver;
using Proteus.Core.Text;
using Proteus.Text.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Cadmus.Export.ML;

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
    private int _group;
    private string? _pendingGroupId;

    /// <summary>
    /// The context block type data key to retrieve it from the context data.
    /// </summary>
    public const string CONTEXT_BLOCK_TYPE = "block-type";

    /// <summary>
    /// The key used in the rendering context to keep track of unique identifiers
    /// for segments like <c>lem</c> and <c>rdg</c>.
    /// </summary>
    public const string CONTEXT_SEG_IDKEY = "text-tree-renderer.tei-app-linear:seg";

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

    private void AddWitDetail(string segName, string? segId, string detail,
        XElement seg, IRendererContext context)
    {
        // witDetail
        XElement witDetail = new(NamespaceOptions.TEI + "witDetail", detail);
        seg.Add(witDetail);

        // @target=segID
        int targetId = context.GetNextIdFor(CONTEXT_SEG_IDKEY);
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
                XElement app = new(NamespaceOptions.TEI + "app");
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
                        ? new XElement(NamespaceOptions.TEI + "rdg")
                        {
                            Value = features.First(f => f.Name ==
                                AppLinearTextTreeFilter.F_APP_E_VARIANT).Value
                        }
                        : new XElement(NamespaceOptions.TEI + "lem")
                        {
                            Value = node.Data.Text!
                        };

                    // corner case: a zero-variant can have a type attribute
                    if (seg.Name.LocalName == "rdg" && seg.Value.Length == 0 &&
                        _options.ZeroVariantType != null)
                    {
                        seg.SetAttributeValue("type", _options.ZeroVariantType);
                    }

                    EnrichSegment(features, seg, context);
                    app.Add(seg);

                    // if there is a note, add a note child element
                    TextSpanFeature? noteFeature = features.FirstOrDefault(
                        f => f.Name == AppLinearTextTreeFilter.F_APP_E_NOTE);
                    if (noteFeature != null)
                    {
                        XElement note = new(NamespaceOptions.TEI + "note",
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

    /// <summary>
    /// Gets or sets the head code template to be rendered at the start of the
    /// each group of items. Its value can include placeholders in curly braces,
    /// corresponding to any of the metadata keys defined in the item composer's
    /// context.
    /// </summary>
    public string? GroupHeadTemplate { get; set; }

    /// <summary>
    /// Gets or sets the tail code template to be rendered at the end of each
    /// group of items. Its value can include placeholders in curly braces,
    /// corresponding to any of the metadata keys defined in the item composer's
    /// context.
    /// </summary>
    public string? GroupTailTemplate { get; set; }
}
