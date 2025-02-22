using Cadmus.Philology.Parts;
using Fusi.Tools.Configuration;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using System.Linq;

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
/// <para>Tag: <c>it.vedph.json-renderer.tei-standoff.apparatus</c>.</para>
/// </summary>
/// <seealso cref="JsonRenderer" />
/// <seealso cref="IJsonRenderer" />
[Tag("it.vedph.json-renderer.tei-standoff.apparatus")]
public sealed class TeiStandoffApparatusJsonRenderer : JsonRenderer,
    IJsonRenderer, IConfigurable<TeiStandoffApparatusJsonRendererOptions>
{
    private readonly JsonSerializerOptions _jsonOptions;

    private TeiStandoffApparatusJsonRendererOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeiStandoffApparatusJsonRenderer"/>
    /// class.
    /// </summary>
    public TeiStandoffApparatusJsonRenderer()
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
        if (!string.IsNullOrEmpty(entry.Value))
        {
            sb.Append(entry.Value);
        }
        else if (entry.Type == ApparatusEntryType.Replacement &&
            !string.IsNullOrEmpty(_options?.ZeroVariant))
        {
            sb.Append(_options.ZeroVariant);
        }

        if (!string.IsNullOrEmpty(entry.Note))
        {
            if (sb.Length > 0) sb.Append(' ');

            if (!string.IsNullOrEmpty(entry.Value) &&
                !string.IsNullOrEmpty(_options?.NotePrefix))
            {
                sb.Append(_options.NotePrefix);
            }
            sb.Append(entry.Note);
        }
        return sb.ToString();
    }

    //private static string RenderAnnotatedValue(AnnotatedValue av)
    //{
    //    StringBuilder sb = new(av.Value);
    //    if (!string.IsNullOrEmpty(av.Note))
    //    {
    //        if (sb.Length > 0) sb.Append(' ');
    //        sb.Append(av.Note);
    //    }
    //    return sb.ToString();
    //}

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(TeiStandoffApparatusJsonRendererOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="json">The input JSON.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>Rendered output.</returns>
    protected override string DoRender(string json,
        IRendererContext? context = null)
    {
        // read fragments array
        JsonNode? root = JsonNode.Parse(json);
        if (root == null) return "";
        ApparatusLayerFragment[]? fragments =
            root["fragments"].Deserialize<ApparatusLayerFragment[]>(_jsonOptions);
        if (fragments == null || context == null) return "";

        // div @xml:id="ITEM_ID"
        XElement itemDiv = new(NamespaceOptions.TEI + "div",
            new XAttribute(NamespaceOptions.XML + "id",
                context.Data[ItemComposer.M_ITEM_ID]));

        // process each fragment
        int frIndex = 0;
        foreach (ApparatusLayerFragment fr in fragments)
        {
            // div @type="TAG"
            XElement frDiv = new(NamespaceOptions.TEI + "div");
            if (!string.IsNullOrEmpty(fr.Tag))
                frDiv.SetAttributeValue("type", fr.Tag);
            itemDiv.Add(frDiv);

            // app @loc="BLOCK_ID"
            // the target block ID must be fetched from the fragment IDs
            // map in context; to get the ID for this fragment, we rely
            // on the current layer ID, get its prefix, and use this to
            // build the map's key (prefix + fragment index). This is done
            // once and reused for each entry in the fragment, as all the
            // entries in it refer to the same location.
            int layerId = (int)context.Data[TeiStandoffItemComposer.M_LAYER_ID];
            string layerPrefix = context.LayerIds.First(
                p => p.Value == layerId).Key;
            string frKey = $"{layerPrefix}{frIndex}";
            string loc = context.FragmentIds[frKey];

            int n = 0;
            foreach (ApparatusEntry entry in fr.Entries)
            {
                // div/app @n="INDEX + 1"
                XElement app = new(NamespaceOptions.TEI + "app",
                    new XAttribute("n", ++n),
                    new XAttribute("loc", "#" + loc));
                frDiv.Add(app);

                // app @type="TAG"
                if (!string.IsNullOrEmpty(entry.Tag))
                    app.SetAttributeValue("type", entry.Tag);

                // div/rdg or div/note with value[+note]
                XElement rdgOrNote = new(NamespaceOptions.TEI +
                    (entry.Type == ApparatusEntryType.Note? "note" : "rdg"),
                    BuildValue(entry));
                app.Add(rdgOrNote);

                // @wit
                if (entry.Witnesses?.Count > 0)
                {
                    rdgOrNote.SetAttributeValue("wit",
                        string.Join(" ", from av in entry.Witnesses
                                         select $"#{av.Value}"));
                }

                // @source
                if (entry.Authors?.Count > 0)
                {
                    rdgOrNote.SetAttributeValue("source",
                        string.Join(" ", from av in entry.Authors
                                         select $"#{av.Value}"));
                }
            }
            frIndex++;
        }

        return itemDiv.ToString(SaveOptions.OmitDuplicateNamespaces)
            // remove default TEI namespace
            .Replace(" xmlns=\"http://www.tei-c.org/ns/1.0\"", "")
            + Environment.NewLine;
    }
}

/// <summary>
/// Options for <see cref="TeiStandoffApparatusJsonRenderer"/>
/// </summary>
public class TeiStandoffApparatusJsonRendererOptions
{
    /// <summary>
    /// Gets or sets the text to output for a zero variant. A zero
    /// variant is a deletion, represented as a text variant with an
    /// empty value. When building an output, you might want to add
    /// some conventional text for it, e.g. <c>del.</c> (delevit),
    /// which is the default value.
    /// </summary>
    public string? ZeroVariant { get; set; }

    /// <summary>
    /// Gets or sets the note prefix. This is an optional string to be
    /// prefixed to the note text after a non-empty value in a <c>rdg</c>
    /// or <c>note</c> element value. Any apparatus entry can have a
    /// value, and an optional note; when it's a variant mostly it has
    /// a value, and eventually a note; when it's a note, mostly it has
    /// only the note without value. The default value is space.
    /// </summary>
    public string? NotePrefix { get; set; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="TeiStandoffApparatusJsonRendererOptions"/> class.
    /// </summary>
    public TeiStandoffApparatusJsonRendererOptions()
    {
        ZeroVariant = "del.";
        NotePrefix = " ";
    }
}
