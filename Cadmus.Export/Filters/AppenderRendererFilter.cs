using Fusi.Tools.Configuration;
using System;

namespace Cadmus.Export.Filters;

/// <summary>
/// Appender renderer filter. This just appends the specified text.
/// <para>Tag: <c>it.vedph.renderer-filter.appender</c>.</para>
/// </summary>
[Tag("it.vedph.renderer-filter.appender")]
public sealed class AppenderRendererFilter : IRendererFilter,
    IConfigurable<AppenderRendererFilterOptions>
{
    private string? _text;

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(AppenderRendererFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _text = options.Text;
    }

    /// <summary>
    /// Applies this filter to the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>The filtered text.</returns>
    public string Apply(string text, IRendererContext? context = null)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(_text))
            return text;

        return text + _text;
    }
}

/// <summary>
/// Options for <see cref="AppenderRendererFilter"/>.
/// </summary>
public class AppenderRendererFilterOptions
{
    /// <summary>
    /// Gets or sets the text to be appended.
    /// </summary>
    public string? Text { get; set; }
}
