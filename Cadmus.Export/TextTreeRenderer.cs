using System;
using System.Collections.Generic;
using Cadmus.Core;
using Cadmus.Core.Layers;
using Cadmus.Export.Filters;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;

namespace Cadmus.Export;

/// <summary>
/// Base class for <see cref="ITextTreeRenderer"/> implementations.
/// </summary>
public abstract class TextTreeRenderer : IHasRendererFilters
{
    /// <summary>
    /// Gets the optional filters to apply after the renderer completes.
    /// </summary>
    public IList<IRendererFilter> Filters { get; } = [];

    /// <summary>
    /// Gets the tag value for the specified object instance decorated
    /// with <see cref="TagAttribute"/>.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <returns>Value or null.</returns>
    /// <exception cref="ArgumentNullException">instance</exception>
    protected static string? GetTagValueFor(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        Type type = instance.GetType();
        TagAttribute? attribute = (TagAttribute?)Attribute.GetCustomAttribute(
            type, typeof(TagAttribute));
        return attribute?.Tag;
    }

    /// <summary>
    /// Gets the fragment ID prefix used in text tree nodes to link fragments.
    /// This is a string like "it.vedph.token-text-layer:fr.it.vedph.comment_".
    /// </summary>
    /// <param name="layerPart">The layer part.</param>
    /// <param name="layerFragment">The layer fragment.</param>
    /// <returns>Prefix.</returns>
    protected static string GetFragmentPrefixFor(IPart layerPart,
        ITextLayerFragment layerFragment)
    {
        return GetTagValueFor(layerPart) + ":" +
            GetTagValueFor(layerFragment) + "_";
    }

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="tree">The root node of the text tree.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>Rendered output.</returns>
    protected abstract string DoRender(TreeNode<TextSpanPayload> tree,
        IRendererContext? context = null);

    /// <summary>
    /// Renders the specified JSON code.
    /// </summary>
    /// <param name="tree">The root node of the text tree.</param>
    /// <param name="context">The optional renderer context.</param>
    /// <returns>Rendered output.</returns>
    /// <exception cref="ArgumentNullException">tree</exception>
    public string Render(TreeNode<TextSpanPayload> tree,
        IRendererContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(tree);

        string result = DoRender(tree, context);

        // apply filters
        foreach (IRendererFilter filter in Filters)
            result = filter.Apply(result, context);

        return result;
    }
}
