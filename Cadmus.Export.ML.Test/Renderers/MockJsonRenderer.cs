using Cadmus.Export.Renderers;
using Fusi.Tools.Data;
using System;

namespace Cadmus.Export.ML.Test.Renderers;

internal sealed class MockJsonRenderer(Func<string, IRendererContext,
    TreeNode<TextSpan>?, string> renderFunc) : JsonRenderer, IJsonRenderer
{
    private readonly Func<string, IRendererContext,
        TreeNode<TextSpan>?, string> _renderFunc =
        renderFunc ?? throw new ArgumentNullException(nameof(renderFunc));

    protected override string DoRender(string json, IRendererContext context,
        TreeNode<TextSpan>? tree = null) => _renderFunc(json, context, tree);
}
