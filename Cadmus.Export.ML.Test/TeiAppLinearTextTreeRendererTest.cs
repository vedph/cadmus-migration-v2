using Cadmus.Core;
using Cadmus.Export.Filters;
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
        AppLinearTextTreeFilter filter = new();
        filter.Apply(tree, item);

        string xml = renderer.Render(tree, new RendererContext
        {
            Item = item
        });

        Assert.NotEmpty(xml);
        // TODO
    }
}
