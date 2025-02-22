using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System.Linq;

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
    /// Gets or sets the text.
    /// </summary>
    public string? Text { get; set; } = range.Text;

    /// <summary>
    /// Gets the features.
    /// </summary>
    public List<TextSpanFeature> Features { get; } = [];

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        List<TextSpanFeature> featuresToDisplay = Features.Count > 10
            ? [.. Features.Take(10)] : Features;
        string featuresString = string.Join(", ", featuresToDisplay);
        if (Features.Count > 10)
            featuresString += $", ... ({Features.Count - 10})";
        return $"{Text}: {featuresString}";
    }
}
