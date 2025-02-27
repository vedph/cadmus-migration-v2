using Cadmus.Export.Filters;
using Fusi.Tools.Configuration;
using System;
using System.Text;

namespace Cadmus.Export.ML.Filters;

/// <summary>
/// Fragments links renderer filter. This filter replaces all the fragment
/// keys delimited between a specified pair of opening and closing tags
/// with a target ID got from the rendering context.
/// For instance, a key like (map/layerTypeId:roleId@fragmentIndex) as
/// <c>seg/it.vedph.token-text-layer:fr.it.vedph.apparatus@0</c>
/// is mapped to a target ID like <c>seg123</c>.
/// <para>Tag: <c>it.vedph.renderer-filter.fr-link</c>.</para>
/// </summary>
[Tag("it.vedph.renderer-filter.fr-link")]
public sealed class FrLinkRendererFilter : IRendererFilter,
    IConfigurable<FrLinkRendererFilterOptions>
{
    private FrLinkRendererFilterOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrLinkRendererFilter"/>
    /// class.
    /// </summary>
    public FrLinkRendererFilter()
    {
        _options = new FrLinkRendererFilterOptions();
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(FrLinkRendererFilterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    private static (string map, string sourceId) ParseKey(string key)
    {
        // a key has form map/layerTypeId:roleId@fragmentIndex
        // optionally followed by a suffix after the fragmentIndex
        // up to the end of the key
        int i = key.IndexOf('/');
        return i == -1
            ? ((string map, string sourceId))("", key)
            : ((string map, string sourceId))(key[..i], key[(i + 1)..]);
    }

    /// <summary>
    /// Applies this filter to the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="context">The rendering context. This is required;
    /// if null, the filter will do nothing.</param>
    /// <returns>The filtered text.</returns>
    public string Apply(string text, IRendererContext? context = null)
    {
        if (string.IsNullOrEmpty(text) || context == null) return text;

        StringBuilder sb = new();
        int start = 0, i = text.IndexOf(_options.TagOpen);
        while (i > -1)
        {
            // prepend left stuff
            if (i > start) sb.Append(text, start, i - start);

            // move to closing tag
            int j = i + _options.TagOpen.Length;
            i = text.IndexOf(_options.TagClose, i);
            if (i == -1) i = text.Length;

            // extract and resolve key if possible
            string key = text[j..i];
            (string map, string sourceId) = ParseKey(key);

            int? id = context.GetMappedId(map, sourceId);
            if (id != null)
            {
                sb.Append(map).Append(id);
            }
            else if (!_options.OmitUnresolved)
            {
                sb.Append(key);
            }

            // move past closing tag
            if (i < text.Length) i += _options.TagClose.Length;
            start = i;

            // move to next opening tag
            i = text.IndexOf(_options.TagOpen, i);
        }

        if (start < text.Length) sb.Append(text, start, text.Length - start);
        return sb.ToString();
    }
}

/// <summary>
/// Options for <see cref="FrLinkRendererFilterOptions"/>.
/// </summary>
public class FrLinkRendererFilterOptions
{
    /// <summary>
    /// Gets or sets the tag opening the fragment key to be mapped.
    /// </summary>
    public string TagOpen { get; set; }

    /// <summary>
    /// Gets or sets the tag closing the fragment key to be mapped.
    /// </summary>
    public string TagClose { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to omit unresolved keys
    /// rather than passing them though.
    /// </summary>
    public bool OmitUnresolved { get; set; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="FrLinkRendererFilterOptions"/> class.
    /// </summary>
    public FrLinkRendererFilterOptions()
    {
        TagOpen = "#[";
        TagClose = "]#";
    }
}
