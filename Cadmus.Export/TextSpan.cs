using Cadmus.Core;
using System;
using System.Linq;
using MongoDB.Driver;

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
        return $"[{Type}] {Text}{(IsBeforeEol ? " [\u21b4]" : "")}";
    }
}
