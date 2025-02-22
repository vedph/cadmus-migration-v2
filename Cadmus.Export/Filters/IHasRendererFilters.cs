using System.Collections.Generic;

namespace Cadmus.Export.Filters;

/// <summary>
/// Interface implemented by components having renderer filters.
/// </summary>
public interface IHasRendererFilters
{
    /// <summary>
    /// Gets the optional filters to apply after the renderer completes.
    /// </summary>
    public IList<IRendererFilter> Filters { get; }
}
