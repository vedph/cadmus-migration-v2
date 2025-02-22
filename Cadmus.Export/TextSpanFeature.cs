using System;

namespace Cadmus.Export;

/// <summary>
/// A generic feature added to a <see cref="TextSpanPayload"/>.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="Value">The value.</param>
/// <param name="Source">The source.</param>
/// <exception cref="ArgumentNullException">name or value</exception>
public record TextSpanFeature(string Name, string Value, string? Source)
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; init; } = Name
        ?? throw new ArgumentNullException(nameof(Name));

    /// <summary>
    /// Gets the value.
    /// </summary>
    public string Value { get; init; } = Value
        ?? throw new ArgumentNullException(nameof(Value));

    /// <summary>
    /// Gets or sets the source for this feature. In most cases this is a
    /// fragment ID.
    /// </summary>
    public string? Source { get; set; } = Source;

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Name}: {Value}" + (Source != null? $" [{Source}]" : "");
    }
}
