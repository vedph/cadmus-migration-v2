using Fusi.Tools.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cadmus.Export.Suppliers;

/// <summary>
/// Flag renderer context supplier. This inspects the item's flags from
/// the received context, and for each flag bitvalue set and mapped in its
/// configuration it supplies a name=value pair to the context data.
/// <para>Tag: <c>renderer-context-supplier.flag</c>.</para>
/// </summary>
/// <seealso cref="IRendererContextSupplier" />
[Tag("renderer-context-supplier.flag")]
public sealed class FlagRendererContextSupplier : IRendererContextSupplier,
    IConfigurable<FlagRendererContextSupplierOptions>
{
    private readonly Dictionary<int, Tuple<string, string>> _map = [];

    /// <summary>
    /// Configures this supplier with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(FlagRendererContextSupplierOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _map.Clear();
        if (options.Mappings == null) return;

        foreach (var kvp in options.Mappings)
        {
            int n;
            if (kvp.Key.StartsWith('H') || kvp.Key.StartsWith('h'))
            {
                if (!int.TryParse(kvp.Value[1..], NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out n)) continue;
            }
            else
            {
                if (!int.TryParse(kvp.Key, out n)) continue;
            }
            int i = kvp.Value.IndexOf('=');
            if (i == -1) continue;
            string name = kvp.Value[..i];
            string value = kvp.Value[(i + 1)..];
            _map[n] = Tuple.Create(name, value);
        }
    }

    /// <summary>
    /// Supplies data to the specified context.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <exception cref="ArgumentNullException">context</exception>
    public void Supply(IRendererContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Item == null || context.Item.Flags == 0) return;

        foreach (int n in _map.Keys)
        {
            if ((context.Item.Flags & n) == n)
            {
                Tuple<string, string> pair = _map[n];
                context.Data[pair.Item1] = pair.Item2;
            }
        }
    }
}

/// <summary>
/// Options for <see cref="FlagRendererContextSupplier"/>.
/// </summary>
public class FlagRendererContextSupplierOptions
{
    /// <summary>
    /// Gets or sets the flag to pair mappings. Keys are flags (decimal values,
    /// or hexadecimal values prefixed by H), values are the corresponding
    /// name=value pair as a string with <c>=</c> as separator.
    /// </summary>
    public IDictionary<string, string>? Mappings { get; set; }
}
