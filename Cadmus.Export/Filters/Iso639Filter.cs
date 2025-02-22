using Fusi.Tools.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Cadmus.Export.Filters;

/// <summary>
/// ISO639-3 or ISO639-2 filter. This is a simple lookup filter replacing
/// these ISO codes with the corresponding English language names.
/// In most cases you will rather use a thesaurus filter, when a subset
/// of these codes are used as thesaurus values, as this ensures that you
/// get the desired name and locale. This is a quick filter used when you
/// just deal with ISO639 codes without recurring to a thesaurus, nor
/// requiring localization.
/// <para>Tag: <c>it.vedph.renderer-filter.iso639</c>.</para>
/// </summary>
/// <seealso cref="IRendererFilter" />
[Tag("it.vedph.renderer-filter.iso639")]
public sealed class Iso639Filter : IRendererFilter,
    IConfigurable<Iso639FilterOptions>
{
    private static Dictionary<string, string>? _code3;
    private static Dictionary<string, string>? _code2;

    private Regex _isoRegex;
    private bool _twoLetters;

    /// <summary>
    /// Initializes a new instance of the <see cref="Iso639Filter"/> class.
    /// </summary>
    public Iso639Filter()
    {
        _isoRegex = new Regex(@"\^\^([a-z]{3})", RegexOptions.Compiled);
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(Iso639FilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _twoLetters = options.TwoLetters;
        _isoRegex = new Regex(options.Pattern, RegexOptions.Compiled);
    }

    private static void LoadCodes()
    {
        if (_code3 != null) _code3.Clear();
        else _code3 = new Dictionary<string, string>();

        if (_code2 != null) _code3.Clear();
        else _code2 = new Dictionary<string, string>();

        using StreamReader reader = new(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Cadmus.Export.Assets.Iso639.txt")!,
            Encoding.UTF8);
        string? line;
        while ((line= reader.ReadLine()) != null)
        {
            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');
            if (cols.Length == 3)
            {
                _code3[cols[0]] = cols[2];
                _code2[cols[1]] = cols[2];
            }
        }
    }

    /// <summary>
    /// Applies this filter to the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="context">The optional rendering context.</param>
    /// <returns>
    /// The filtered text.
    /// </returns>
    public string Apply(string text, IRendererContext? context = null)
    {
        if (string.IsNullOrEmpty(text) || _isoRegex == null) return text;

        if (_code3 == null) LoadCodes();

        return _isoRegex.Replace(text, (Match m) =>
        {
            string code = m.Groups[1].Value.ToLowerInvariant();
            Dictionary<string, string> map = _twoLetters ? _code2! : _code3!;
            return map.ContainsKey(code) ? map[code] : code;
        });
    }
}

/// <summary>
/// Options for <see cref="Iso639Filter"/>.
/// </summary>
public class Iso639FilterOptions
{
    /// <summary>
    /// Gets or sets the pattern used to identify ISO codes. It is assumed
    /// that the code is the first captured group in a match. Default
    /// is <c>^^</c> followed by 3 letters for ISO 639-3.
    /// </summary>
    public string Pattern { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use two letter codes
    /// instead of 3 letter codes.
    /// </summary>
    public bool TwoLetters { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Iso639FilterOptions"/>
    /// class.
    /// </summary>
    public Iso639FilterOptions()
    {
        Pattern = @"\^\^([a-z]{3})";
    }
}
