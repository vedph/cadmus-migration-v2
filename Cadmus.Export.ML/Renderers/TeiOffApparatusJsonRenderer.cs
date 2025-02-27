using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Cadmus.Export.Renderers;
using Proteus.Core.Text;
using Fusi.Tools.Data;
using Cadmus.General.Parts;
using Cadmus.Core;

namespace Cadmus.Export.ML.Renderers;

/// <summary>
/// JSON renderer for standoff TEI apparatus layer part. This works in tandem
/// with <see cref="TeiOffLinearTextTreeRenderer"/>, whose task is building the
/// base text referenced by the apparatus entries.
/// </summary>
/// <seealso cref="JsonRenderer" />
/// <seealso cref="IJsonRenderer" />
[Tag("it.vedph.json-renderer.tei-off.apparatus")]
public sealed class TeiOffApparatusJsonRenderer : MLJsonRenderer,
    IJsonRenderer, IConfigurable<AppLinearTextTreeRendererOptions>
{
    private readonly JsonSerializerOptions _jsonOptions;

    private AppLinearTextTreeRendererOptions _options;
    private IRendererContext? _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeiOffApparatusJsonRenderer"/>
    /// class.
    /// </summary>
    public TeiOffApparatusJsonRenderer()
    {
        _jsonOptions = new()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
        };
        _options = new();
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(AppLinearTextTreeRendererOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    private void AddWitDetail(string attrName, string? witOrResp, string sourceId,
        string detail, XElement lemOrRdg)
    {
        // witDetail
        XElement witDetail = new(NamespaceOptions.TEI + "witDetail", detail);
        lemOrRdg.Add(witDetail);

        // @target=lem or rdg ID
        string local = lemOrRdg.Name.LocalName;
        int targetId = _context!.MapSourceId(local, sourceId);
        lemOrRdg.SetAttributeValue(_options.ResolvePrefixedName("xml:id"),
            $"{local}{targetId}");
        witDetail.SetAttributeValue("target", $"#{local}{targetId}");

        // @wit or @resp
        if (witOrResp != null)
            witDetail.SetAttributeValue(attrName, $"#{witOrResp}");
    }

    private void AddWitOrResp(string sourceId, ApparatusEntry entry,
        XElement lemOrRdg)
    {
        StringBuilder wit = new();
        StringBuilder resp = new();

        foreach (AnnotatedValue av in entry.Witnesses)
        {
            if (wit.Length > 0) wit.Append(' ');
            wit.Append('#').Append(av.Value);
            if (!string.IsNullOrEmpty(av.Note))
                AddWitDetail("wit", av.Value, sourceId, av.Note, lemOrRdg);
        }

        foreach (LocAnnotatedValue lav in entry.Authors)
        {
            if (resp.Length > 0) resp.Append(' ');
            resp.Append('#').Append(lav.Value);
            if (!string.IsNullOrEmpty(lav.Note))
                AddWitDetail("resp", lav.Value, sourceId, lav.Note, lemOrRdg);
        }

        if (wit.Length > 0)
            lemOrRdg.SetAttributeValue("wit", wit.ToString());
        if (resp.Length > 0)
            lemOrRdg.SetAttributeValue("resp", resp.ToString());
    }

    private XElement? BuildAppElement(string textPartId,
        ApparatusLayerFragment fr, int frIndex,
        TreeNode<TextSpanPayload> tree)
    {
        // calculate the apparatus fragment ID prefix
        // (like "it.vedph.token-text-layer:fr.it.vedph.comment@INDEX")
        string prefix = TextSpanPayload.GetFragmentPrefixFor(
            new TokenTextLayerPart<ApparatusLayerFragment>(),
            new ApparatusLayerFragment()) + frIndex;

        // find first and last nodes having a fragment ID starting with prefix
        var bounds = FindFragmentBounds(prefix, tree);
        if (bounds == null) return null;

        // collect text from nodes
        StringBuilder text = new();
        bounds.Value.First.Traverse(node =>
        {
            if (node.Data != null)
                text.Append(node.Data.Text);
            return node != bounds.Value.Last;
        });

        // div/app @n="FRINDEX+1"
        XElement app = new(NamespaceOptions.TEI + "app",
            new XAttribute("n", frIndex + 1));

        // div/app @type="TAG"
        if (!string.IsNullOrEmpty(fr.Tag))
            app.SetAttributeValue("type", fr.Tag);

        // div/app @loc="segID" or loc @spanFrom/spanTo
        AddTeiLocToElement(textPartId, bounds.Value.First, bounds.Value.Last, app,
            _context!);

        // for each entry
        int entryIndex = 0;
        foreach (ApparatusEntry entry in fr.Entries)
        {
            // if it has a variant render rdg, else render lem
            XElement lemOrRdg = entry.Value != null
                ? new(NamespaceOptions.TEI + "rdg", entry.Value)
                : new(NamespaceOptions.TEI + "lem", text.ToString());

            // rdg or lem @n="ENTRY_INDEX+1"
            lemOrRdg.SetAttributeValue("n", entryIndex + 1);
            app.Add(lemOrRdg);

            // rdg or lem @type="TAG"
            if (!string.IsNullOrEmpty(entry.Tag))
                lemOrRdg.SetAttributeValue("type", entry.Tag);

            // rdg or lem/note
            if (!string.IsNullOrEmpty(entry.Note))
            {
                lemOrRdg.Add(new XElement(NamespaceOptions.TEI + "note",
                    entry.Note));
            }

            // rdg or lem/@wit or @resp
            AddWitOrResp($"{textPartId}_{frIndex}.{entryIndex}", entry,
                lemOrRdg);

            entryIndex++;
        }

        return app;
    }

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="json">The input JSON.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <param name="tree">The optional text tree. This is used for layer
    /// fragments to get source IDs targeting the various portions of the
    /// text.</param>
    /// <returns>Rendered output.</returns>
    /// <exception cref="InvalidOperationException">null tree</exception>
    protected override string DoRender(string json,
        IRendererContext context, TreeNode<TextSpanPayload>? tree = null)
    {
        if (tree == null)
        {
            throw new InvalidOperationException("Text tree is required " +
                "for rendering standoff apparatus");
        }

        IPart? textPart = context.GetTextPart();
        if (textPart == null) return "";

        _context = context;

        // read fragments array
        JsonNode? root = JsonNode.Parse(json);
        if (root == null) return "";
        ApparatusLayerFragment[]? fragments =
            root["fragments"].Deserialize<ApparatusLayerFragment[]>(_jsonOptions);
        if (fragments == null || context == null) return "";

        // div @xml:id="item<ID>"
        // get the root element name (usually div)
        XName rootName = _options.ResolvePrefixedName(_options.RootElement);

        XElement itemDiv = new(rootName,
            new XAttribute(NamespaceOptions.XML + "id",
            $"item{context.Item!.Id}"));

        // process each fragment
        for (int frIndex = 0; frIndex < fragments.Length; frIndex++)
        {
            ApparatusLayerFragment fr = fragments[frIndex];

            // div @type="TAG"
            XElement frDiv = new(NamespaceOptions.TEI + "div");
            if (!string.IsNullOrEmpty(fr.Tag))
                frDiv.SetAttributeValue("type", fr.Tag);
            itemDiv.Add(frDiv);

            foreach (ApparatusEntry entry in fr.Entries)
            {
                // div/app @n="INDEX + 1"
                XElement? app = BuildAppElement(textPart.Id, fr, frIndex, tree);
                if (app != null) frDiv.Add(app);
            }
        }

        _context = null;

        return itemDiv.ToString(_options.IsIndented
            ? SaveOptions.OmitDuplicateNamespaces
            : SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces);
    }
}
