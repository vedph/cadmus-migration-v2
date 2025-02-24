using Cadmus.Core.Layers;
using Cadmus.Core;
using Fusi.Tools.Configuration;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;

namespace Cadmus.Export;

/// <summary>
/// The payload added to nodes during text export.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TextSpanPayload"/> class.
/// </remarks>
/// <param name="range">The source fragment text range for this payload.</param>
public class TextSpanPayload(FragmentTextRange range)
{
    /// <summary>
    /// Gets the source fragment text range for this payload.
    /// </summary>
    public FragmentTextRange Range { get; } = range;

    /// <summary>
    /// Gets or sets the optional node type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this span was before and end
    /// of line marker (LF) in the source text.
    /// </summary>
    public bool IsBeforeEol { get; set; }

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string? Text { get; set; } = range.Text;

    /// <summary>
    /// Gets the features.
    /// </summary>
    public List<TextSpanFeature> Features { get; init; } = [];

    /// <summary>
    /// Gets the tag value for the specified object instance decorated
    /// with <see cref="TagAttribute"/>.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <returns>Value or null.</returns>
    /// <exception cref="ArgumentNullException">instance</exception>
    public static string? GetTagAttributeValue(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        Type type = instance.GetType();
        TagAttribute? attribute = (TagAttribute?)Attribute.GetCustomAttribute(
            type, typeof(TagAttribute));
        return attribute?.Tag;
    }

    /// <summary>
    /// Gets the fragment ID prefix used in text tree nodes to link fragments.
    /// This is a string like "it.vedph.token-text-layer:fr.it.vedph.comment@".
    /// </summary>
    /// <param name="layerPart">The layer part.</param>
    /// <param name="layerFragment">The layer fragment.</param>
    /// <returns>Prefix.</returns>
    public static string GetFragmentPrefixFor(IPart layerPart,
        ITextLayerFragment layerFragment)
    {
        return GetTagAttributeValue(layerPart) + ":" +
               GetTagAttributeValue(layerFragment) + "@";
    }

    /// <summary>
    /// Determines whether this span has any feature derived from the type
    /// of fragment defined by the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix (with form TypeId:RoleId@).</param>
    /// <returns>
    ///   <c>true</c> if the specified prefix has fragment; otherwise, <c>false</c>.
    /// </returns>
    public bool HasFeaturesForFragment(string prefix)
    {
        return Features.Any(f => f.Source?.StartsWith(prefix) == true);
    }

    /// <summary>
    /// Gets all the features belonging to the type of fragment defined by
    /// the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <param name="sort">True to sort results.</param>
    /// <returns>Tuples with feature source and feature, optionally sorted
    /// by type ID, role ID, index, and suffix.</returns>
    /// <exception cref="ArgumentNullException">prefix</exception>
    public List<Tuple<FragmentFeatureSource, TextSpanFeature>> GetFragmentFeatures
        (string prefix, bool sort = false)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        var results = Features.Where(f => f.Source?.StartsWith(prefix) == true)
            .Select(f => new Tuple<FragmentFeatureSource, TextSpanFeature>(
                FragmentFeatureSource.Parse(f.Source!), f));

        if (sort)
        {
            results = results.OrderBy(t => t.Item1.TypeId)
                .ThenBy(t => t.Item1.RoleId)
                .ThenBy(t => t.Item1.Index)
                .ThenBy(t => t.Item1.Suffix);
        }

        return [..results];
    }

    /// <summary>
    /// Create a shallow clone of this instance.
    /// </summary>
    /// <returns>Cloned instance.</returns>
    public TextSpanPayload Clone()
    {
        return new TextSpanPayload(Range)
        {
            Type = Type,
            IsBeforeEol = IsBeforeEol,
            Text = Text,
            Features = [..Features]
        };
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        List<TextSpanFeature> featuresToDisplay = Features.Count > 5
            ? [.. Features.Take(5)] : Features;
        string featuresString = string.Join(", ", featuresToDisplay);
        if (Features.Count > 5)
            featuresString += $", ... ({Features.Count - 5})";

        return $"[{Type}] {Text}{(IsBeforeEol? "\u21b4" : "")}: {featuresString}";
    }
}
