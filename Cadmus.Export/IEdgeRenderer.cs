namespace Cadmus.Export;

/// <summary>
/// "Edge" renderer: this is used by an <see cref="IItemComposer"/>
/// to render the head and tail of its outputs. For instance, if the composer
/// outputs a file with TEI text and another one with a TEI apparatus,
/// the edge renderer can be used to render the head and tail of a TEI
/// body.
/// </summary>
public interface IEdgeRenderer
{
    /// <summary>
    /// Opens the renderer for the output with the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="target">The target of the edges to render. This varies
    /// according to the output; for instance, it might be the path of a
    /// file whose body has been written earlier.</param>
    /// <param name="context">The context.</param>
    void Open(string key, string target, RendererContext context);

    /// <summary>
    /// Notifies the renderer that a new item is being processed.
    /// </summary>
    /// <param name="context">The context.</param>
    void Next(RendererContext context);

    /// <summary>
    /// Closes the renderer.
    /// </summary>
    void Close();
}
