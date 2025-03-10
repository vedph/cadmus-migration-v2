using Cadmus.Core;
using System;
using System.Linq;
using MongoDB.Driver;
using System.Collections.Generic;
using Fusi.Tools.Data;
using System.Text;

namespace Cadmus.Export;

/// <summary>
/// A span of text derived from a range and added as payload to nodes during
/// the rendition process.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TextSpan"/> class.
/// </remarks>
/// <param name="range">The source fragment text range for this payload.</param>
public class TextSpan(AnnotatedTextRange? range = null)
{
    /// <summary>
    /// Gets the source fragment text range for this payload.
    /// This is null for the root node, all the other nodes should have one.
    /// </summary>
    public AnnotatedTextRange? Range { get; } = range;

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
    public string? Text { get; set; } = range?.Text;

    /// <summary>
    /// Gets or sets the features attached to this span. A feature is a generic
    /// name=value pair, and a span can also have multiple features with the
    /// same name. Such features may be arbitrarily injected during the rendition
    /// process.
    /// </summary>
    public IList<TextSpanFeature>? Features { get; set; }

    /// <summary>
    /// Gets the fragment ID prefix used in text tree nodes to link fragments.
    /// This is a string like "it.vedph.token-text-layer:fr.it.vedph.comment@".
    /// </summary>
    /// <param name="layerPart">The layer part.</param>
    /// <returns>Prefix.</returns>
    public static string GetFragmentPrefixFor(IPart layerPart) =>
        $"{layerPart.TypeId}:{layerPart.RoleId}@";

    /// <summary>
    /// Gets the index of the fragment from its ID, with form
    /// <c>typeId:roleId@index</c> where index might be followed by a suffix.
    /// </summary>
    /// <param name="fragmentId">The fragment identifier.</param>
    /// <returns>Fragment index.</returns>
    /// <exception cref="ArgumentNullException">fragmentId</exception>
    /// <exception cref="FormatException">invalid fragment ID</exception>
    public static int GetFragmentIndex(string fragmentId)
    {
        ArgumentNullException.ThrowIfNull(fragmentId);
        int i = fragmentId.IndexOf('@');
        if (i < 0) throw new FormatException("Invalid fragment ID: " + fragmentId);
        int j = ++i;
        while (j < fragmentId.Length && char.IsDigit(fragmentId[j])) j++;
        return int.Parse(fragmentId[i..j]);
    }

    /// <summary>
    /// Determines whether this span is linked to a fragment with the specified
    /// fragment ID prefix, returning the ID it if found.
    /// </summary>
    /// <param name="prefix">The fragment ID prefix (with form TypeId:RoleId@).
    /// </param>
    /// <returns>The fragment ID of the type specified by prefix, or null if
    /// not found.</returns>
    public string? GetLinkedFragmentId(string prefix) =>
        Range?.FragmentIds.FirstOrDefault(s => s.StartsWith(prefix));

    /// <summary>
    /// Adds the specified feature. If the features list property is null,
    /// the list will be created before adding the feature to it.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentNullException">name or value</exception>
    public void AddFeature(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        Features ??= [];
        Features.Add(new TextSpanFeature(name, value));
    }

    /// <summary>
    /// Removes all the features matching the specified name or name and value,
    /// when value is not null.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The optional value.</param>
    /// <exception cref="ArgumentNullException">name</exception>
    public void RemoveFeatures(string name, string? value = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (Features == null) return;
        Features = [.. Features.Where(f => f.Name != name ||
            (value != null && f.Value != value))];
    }

    /// <summary>
    /// Determines whether this span has any features with the specified name
    /// or name and value when <paramref name="value"/> is not null.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The optional value. If not specified, only the
    /// name is matched.</param>
    /// <returns>
    ///   <c>true</c> if the specified name has the feature; otherwise,
    ///   <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">name</exception>
    public bool HasFeature(string name, string? value = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (Features == null) return false;
        return Features.Any(f => f.Name == name &&
            (value == null || f.Value == value));
    }

    /// <summary>
    /// Create a clone of this instance.
    /// </summary>
    /// <returns>Cloned instance.</returns>
    public TextSpan Clone()
    {
        return new TextSpan(Range)
        {
            Type = Type,
            IsBeforeEol = IsBeforeEol,
            Text = Text,
            Features = Features != null? [..Features] : null,
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
        StringBuilder sb = new();

        // type
        if (!string.IsNullOrEmpty(Type)) sb.Append($"[{Type}] ");

        // text
        sb.Append(DumpHelper.MapNonPrintables(Text, true));

        // end of line
        if (IsBeforeEol) sb.Append(" [\u21b4]");

        // features
        if (Features?.Count > 0)
        {
            sb.Append(" F").Append(Features.Count).Append(": ");
            if (Features.Count <= 10)
            {
                sb.AppendJoin(", ", Features);
            }
            else
            {
                sb.Append(" [");
                sb.AppendJoin(", ", Features.Take(10));
                sb.Append(", ...]");
            }
        }

        return sb.ToString();
    }
}
