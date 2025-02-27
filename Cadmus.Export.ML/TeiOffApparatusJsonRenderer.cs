using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using System.Linq;
using Cadmus.Export.Renderers;
using Proteus.Core.Text;
using Fusi.Tools.Data;
using Cadmus.General.Parts;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Cadmus.Export.ML;

/// <summary>
/// JSON renderer for standoff TEI apparatus layer part.
/// This provides apparatus fragments entries rendition. Typically, the
/// TEI apparatus is encoded inside a <c>standOff</c> element in <c>body</c>.
/// The <c>@type</c> attribute of this element contains the apparatus layer
/// part role ID (=fragment's type ID).
/// <para>In <c>standOff</c> there is a <c>div</c> for each item, with
/// <c>@xml:id</c> equal to the item's ID.</para>
/// <para>In this <c>div</c>, each fragment is another <c>div</c> with
/// an optional <c>@type</c> attribute equal to the fragment's tag, when
/// present.</para>
/// <para>Each entry in the fragment is an <c>app</c> entry with these
/// properties (I add to each its corresponding XML rendition):</para>
/// <list type="bullet">
/// <item>
/// <term>type</term>
/// <description>a <c>rdg</c> child element or a <c>note</c> child element
/// according to the type.</description>
/// </item>
/// <item>
/// <term>value</term>
/// <description>the value of <c>rdg</c>/<c>note</c>.</description>
/// </item>
/// <item>
/// <term>tag</term>
/// <description><c>@type</c> of <c>rdg</c>/<c>note</c>.</description>
/// </item>
/// <item>
/// <term>note</term>
/// <description>append to <c>note</c> if existing, else add child <c>note</c>
/// with this content.</description>
/// </item>
/// <item>
/// <term>witnesses</term>
/// <description><c>@wit</c> of <c>rdg</c>/<c>note</c>.</description>
/// </item>
/// <item>
/// <term>authors</term>
/// <description><c>@source</c> of <c>rdg</c>/<c>note</c>.</description>
/// </item>
/// </list>
/// <item>
/// <term>not rendered</term>
/// <description>normValue, isAccepted, groupId.</description>
/// </item>
/// <para>Tag: <c>it.vedph.json-renderer.tei-off.apparatus</c>.</para>
/// </summary>
/// <seealso cref="JsonRenderer" />
/// <seealso cref="IJsonRenderer" />
[Tag("it.vedph.json-renderer.tei-off.apparatus")]
public sealed class TeiOffApparatusJsonRenderer : JsonRenderer,
    IJsonRenderer, IConfigurable<AppLinearTextTreeRendererOptions>
{
    private readonly JsonSerializerOptions _jsonOptions;

    private AppLinearTextTreeRendererOptions _options;

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

    private string BuildValue(ApparatusEntry entry)
    {
        StringBuilder sb = new();
        //if (!string.IsNullOrEmpty(entry.Value))
        //{
        //    sb.Append(entry.Value);
        //}
        //else if (entry.Type == ApparatusEntryType.Replacement &&
        //    !string.IsNullOrEmpty(_options?.ZeroVariant))
        //{
        //    sb.Append(_options.ZeroVariant);
        //}

        //if (!string.IsNullOrEmpty(entry.Note))
        //{
        //    if (sb.Length > 0) sb.Append(' ');

        //    if (!string.IsNullOrEmpty(entry.Value) &&
        //        !string.IsNullOrEmpty(_options?.NotePrefix))
        //    {
        //        sb.Append(_options.NotePrefix);
        //    }
        //    sb.Append(entry.Note);
        //}
        return sb.ToString();
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

    private static (TreeNode<TextSpanPayload> First, TreeNode<TextSpanPayload> Last)?
        FindFragmentBounds(string prefix, TreeNode<TextSpanPayload> tree)
    {
        // find the first and last nodes having any fragment ID starting with prefix
        TreeNode<TextSpanPayload>? firstNode = null;
        TreeNode<TextSpanPayload>? lastNode = null;

        tree.Traverse(node =>
        {
            if (node.Data == null) return true;
            if (node.Data.Range.FragmentIds.Any(s => s.StartsWith(prefix)))
            {
                if (firstNode == null)
                {
                    firstNode = node;
                }
                else
                {
                    lastNode = node;
                    return false;
                }
            }
            return true;
        });

        return lastNode != null? (firstNode!, lastNode!) : null;
    }

    private static void AddLoc(IRendererContext context, int frIndex,
        TreeNode<TextSpanPayload> tree, XElement target)
    {
        // calculate the apparatus fragment ID prefix
        // (like "it.vedph.token-text-layer:fr.it.vedph.comment@INDEX")
        string prefix = TextSpanPayload.GetFragmentPrefixFor(
            new TokenTextLayerPart<ApparatusLayerFragment>(),
            new ApparatusLayerFragment()) + frIndex;

        var bounds = FindFragmentBounds(prefix, tree);
        if (bounds == null) return;

        string firstSourceId = bounds.Value.First.Id!;
        string lastSourceId = bounds.Value.Last.Id!;

        int firstId = context.MapSourceId("seg", firstSourceId);
        int lastId = context.MapSourceId("seg", lastSourceId);

        if (firstId == lastId)
        {
            target.SetAttributeValue("loc", firstSourceId);
        }
        else
        {
            XElement loc = new(NamespaceOptions.TEI + "loc",
                new XAttribute("spanFrom", firstSourceId),
                new XAttribute("spanTo", lastSourceId));
            target.Add(loc);
        }
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

            int n = 0;
            foreach (ApparatusEntry entry in fr.Entries)
            {
                // div/app @n="INDEX + 1"
                XElement app = new(NamespaceOptions.TEI + "app",
                    new XAttribute("n", ++n));

                // div/app @loc="segID" or loc @spanFrom/spanTo
                AddLoc(context, frIndex, tree, app);
                frDiv.Add(app);

                // div/app @type="TAG"
                if (!string.IsNullOrEmpty(entry.Tag))
                    app.SetAttributeValue("type", entry.Tag);

                // div/app/rdg or div/app/note with value[+note]
                XElement rdgOrNote = new(NamespaceOptions.TEI +
                    (entry.Type == ApparatusEntryType.Note? "note" : "rdg"),
                    BuildValue(entry));
                app.Add(rdgOrNote);

                // rdg/note @wit
                if (entry.Witnesses?.Count > 0)
                {
                    rdgOrNote.SetAttributeValue("wit",
                        string.Join(" ", from av in entry.Witnesses
                                         select $"#{av.Value}"));
                }

                // rdg/note @source
                if (entry.Authors?.Count > 0)
                {
                    rdgOrNote.SetAttributeValue("source",
                        string.Join(" ", from av in entry.Authors
                                         select $"#{av.Value}"));
                }
            }
        }

        return itemDiv.ToString(_options.IsIndented
            ? SaveOptions.OmitDuplicateNamespaces
            : SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces);
    }
}
