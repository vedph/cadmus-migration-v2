using Cadmus.Core;
using Cadmus.Export.Filters;
using Cadmus.Export.ML.Renderers;
using Cadmus.Export.Test.Filters;
using Fusi.Tools.Data;
using Xunit;

namespace Cadmus.Export.ML.Test.Renderers;

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

        Assert.Equal("<p xmlns=\"http://www.tei-c.org/ns/1.0\">" +
            "<app><lem wit=\"#O1\">illuc</lem><rdg wit=\"#O #G #R\">illud</rdg>" +
            "<rdg xml:id=\"seg1\" resp=\"#Fruterius\">illic" +
            "<witDetail target=\"#seg1\" resp=\"#Fruterius\">(†1566) 1605a 388" +
            "</witDetail></rdg></app> unde negant redire " +
            "<app><lem wit=\"#O #G\">quemquam</lem><rdg wit=\"#R\">umquam</rdg>" +
            "<note>some note</note></app></p>", xml);
    }
}
