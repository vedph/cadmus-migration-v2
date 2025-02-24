using Cadmus.Core;
using Cadmus.Export.Test.Filters;
using Fusi.Tools.Data;
using Xunit;

namespace Cadmus.Export.ML.Test;

public sealed class TeiAppLinearTextTreeRendererTest
{
    [Fact]
    public void Render_Ok()
    {
        TeiAppLinearTextTreeRenderer renderer = new();

        (TreeNode<TextSpanPayload>? tree, IItem item) =
            AppLinearTextTreeFilterTest.GetTreeAndItem();

        string xml = renderer.Render(tree, new RendererContext
        {
            Item = item
        });

        Assert.NotEmpty(xml);
        // TODO
    }
}
