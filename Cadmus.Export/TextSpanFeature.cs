using System;

namespace Cadmus.Export;

/// <summary>
/// A feature optionally present in a <see cref="TextSpan"/>.
/// </summary>
public record TextSpanFeature
{
    /// <summary>
    /// Gets the feature name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the feature value.
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextSpanFeature"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentNullException">name or value</exception>
    public TextSpanFeature(string name, string value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Name}={Value}";
    }
}
