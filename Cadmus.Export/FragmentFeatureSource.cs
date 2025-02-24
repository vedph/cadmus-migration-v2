using System;
using System.Text.RegularExpressions;

namespace Cadmus.Export;

/// <summary>
/// The source of a <see cref="TextSpanFeature"/> derived from a fragment.
/// The corresponding string in the feature has format <c>TypeId:RoleId@Index</c>
/// or <c>TypeId:RoleId@IndexSuffix</c>.
/// </summary>
public partial record FragmentFeatureSource
{
    /// <summary>
    /// Gets the layer part type identifier.
    /// </summary>
    public string TypeId { get; }

    /// <summary>
    /// Gets the layer part role identifier (=fragment type identifier).
    /// </summary>
    public string RoleId { get; }

    /// <summary>
    /// Gets the fragment's index in the layer part array of fragments.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets an optional suffix used to further identify data inside the
    /// fragment (e.g. an entry index in an apparatus fragment's entries
    /// array).
    /// </summary>
    public string? Suffix { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FragmentFeatureSource"/> class.
    /// </summary>
    /// <param name="typeId">The type identifier.</param>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="index">The index.</param>
    /// <param name="suffix">The optional suffix.</param>
    public FragmentFeatureSource(string typeId, string roleId, int index,
        string? suffix = null)
    {
        TypeId = typeId;
        RoleId = roleId;
        Index = index;
        Suffix = suffix;
    }

    /// <summary>
    /// Converts to a parsable string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{TypeId}:{RoleId}@{Index}" + (Suffix ?? "");
    }

    [GeneratedRegex(@"^(?<t>[^:]+):(?<r>[^\@]+)(?:\@(?<i>[0-9]+)(?:(?<s>.+))?)?$")]
    private static partial Regex ParseRegex();

    /// <summary>
    /// Parses the specified text representing the source of a feature
    /// derived from a fragment.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>Parsed source.</returns>
    /// <exception cref="ArgumentNullException">text</exception>
    /// <exception cref="FormatException">Invalid feature source format or
    /// Invalid index in feature source</exception>
    public static FragmentFeatureSource Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // TypeId:RoleId_Index(.Suffix)
        Match m = ParseRegex().Match(text);

        if (!m.Success)
            throw new FormatException($"Invalid feature source format: \"{text}\"");

        int index = 0;
        if (m.Groups["i"].Success && !int.TryParse(m.Groups["i"].Value, out index))
        {
            throw new FormatException(
                $"Invalid index in feature source: \"{m.Groups["i"].Value}\"");
        }

        return new FragmentFeatureSource(
            m.Groups["t"].Value,
            m.Groups["r"].Value,
            index,
            m.Groups["s"].Success ? m.Groups["s"].Value : null);
    }
}
