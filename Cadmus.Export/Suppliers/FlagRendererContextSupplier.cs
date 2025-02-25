using Fusi.Tools.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cadmus.Export.Suppliers;

public sealed class FlagRendererContextSupplier : IRendererContextSupplier,
    IConfigurable<FlagRendererContextSupplierOptions>
{
    public void Configure(FlagRendererContextSupplierOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        throw new NotImplementedException();
    }

    public void Supply(IRendererContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Item == null) return;

        // TODO
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
    public Dictionary<string, string>? Mappings { get; set; }
}
