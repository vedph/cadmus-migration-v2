using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadmus.Export;

/// <summary>
/// A set of features for a text span.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TextSpanFeatureSet"/> class.
/// </remarks>
/// <param name="key">The key.</param>
/// <param name="source">The source.</param>
/// <exception cref="ArgumentNullException">key or source</exception>
public class TextSpanFeatureSet(string key, string source)
{
    /// <summary>
    /// Gets the key for this set, unique within all the sets derived from
    /// a source fragment. For instance, in an apparatus fragment having many
    /// entries, the key for the features from each entry could be <c>entry.0</c>,
    /// <c>entry.1</c>, etc. Use dots to separate levels.
    /// </summary>
    public string Key { get; } = key
        ?? throw new ArgumentNullException(nameof(key));

    /// <summary>
    /// Gets the source for this set. This is a string identifying the source
    /// fragment as per <see cref="FragmentFeatureSource"/>.
    /// </summary>
    public string Source { get; } = source
        ?? throw new ArgumentNullException(nameof(source));

    /// <summary>
    /// Gets the features in the set.
    /// </summary>
    public List<TextSpanFeature> Features { get; } = [];

    /// <summary>
    /// Clones this set.
    /// </summary>
    /// <returns>Cloned set.</returns>
    public TextSpanFeatureSet Clone()
    {
        TextSpanFeatureSet clone = new(Key, Source);
        clone.Features.AddRange(Features);
        return clone;
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Key}: {Features.Count} [{Source}]";
    }
}
