using Cadmus.Export.Filters;
using Fusi.Tools.Configuration;
using System.Collections.Generic;

namespace Cadmus.Export;

/// <summary>
/// Null JSON renderer. This just returns the received JSON, and can be
/// used for diagnostic purposes, or to apply some filters to the received
/// text.
/// <para>Tag: <c>it.vedph.json-renderer.null</c>.</para>
/// </summary>
/// <seealso cref="IJsonRenderer" />
[Tag("it.vedph.json-renderer.null")]
public sealed class NullJsonRenderer : JsonRenderer, IJsonRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullJsonRenderer"/>
    /// class.
    /// </summary>
    public NullJsonRenderer()
    {
        Filters = new List<IRendererFilter>();
    }

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="json">The input JSON.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>Rendered output.</returns>
    protected override string DoRender(string json,
        IRendererContext? context = null)
    {
        return json ?? "";
    }
}
