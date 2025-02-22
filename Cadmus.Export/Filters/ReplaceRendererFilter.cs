using Fusi.Tools.Configuration;
using Fusi.Tools.Text;
using System;
using System.Collections.Generic;

namespace Cadmus.Export.Filters;

/// <summary>
/// Generic text replacement renderer filter.
/// <para>Tag: <c>it.vedph.renderer-filter.replace</c>.</para>
/// </summary>
[Tag("it.vedph.renderer-filter.replace")]
public sealed class ReplaceRendererFilter : IRendererFilter,
    IConfigurable<ReplaceRendererFilterOptions>
{
    private readonly TextReplacer _replacer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplaceRendererFilter"/>
    /// class.
    /// </summary>
    public ReplaceRendererFilter()
    {
        _replacer = new(false);
    }

    /// <summary>
    /// Configures the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(ReplaceRendererFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _replacer.Clear();
        if (options.Replacements?.Count > 0)
        {
            foreach (var o in options.Replacements)
            {
                if (o.IsPattern)
                    _replacer.AddExpression(o.Source!, o.Target!, o.Repetitions);
                else
                    _replacer.AddLiteral(o.Source!, o.Target!, o.Repetitions);
            }
        }
    }

    /// <summary>
    /// Applies this filter to the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>The filtered text.</returns>
    public string Apply(string text, IRendererContext? context = null)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return _replacer.Replace(text)!;
    }
}

/// <summary>
/// A general purpose set of options for replacing text.
/// </summary>
public class ReplaceEntryOptions
{
    /// <summary>
    /// Gets or sets the source text or pattern.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the target text.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the max repetitions count, or 0 for no limit
    /// (=keep replacing until no more changes).
    /// </summary>
    public int Repetitions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Source"/> is a
    /// regular expression pattern.
    /// </summary>
    public bool IsPattern { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{(IsPattern ? "*" : "")} {Source} => {Target} (×{Repetitions})";
    }
}

/// <summary>
/// Options for <see cref="ReplaceRendererFilter"/>.
/// </summary>
public class ReplaceRendererFilterOptions
{
    /// <summary>
    /// Gets or sets the replacements.
    /// </summary>
    public IList<ReplaceEntryOptions>? Replacements { get; set; }
}
