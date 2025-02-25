namespace Cadmus.Export;

/// <summary>
/// A data supplier for <see cref="IRendererContext"/>'s. This is typically
/// used to extract dictionary-like data from a context and supply it to
/// the renderer context. For instance, you might map an item's flag to
/// some renderer context dictionary name=value pair.
/// </summary>
public interface IRendererContextSupplier
{
    /// <summary>
    /// Supplies data to the specified context.
    /// </summary>
    /// <param name="context">The context.</param>
    public void Supply(IRendererContext context);
}
