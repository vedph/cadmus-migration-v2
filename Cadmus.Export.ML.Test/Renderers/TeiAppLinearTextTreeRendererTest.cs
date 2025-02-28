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

        Assert.Equal("<p xmlns=\"http://www.tei-c.org/ns/1.0\"><app n=\"1\">" +
            "<lem n=\"1\" wit=\"#O1\">illuc</lem><rdg n=\"2\" wit=\"#O #G #R\">" +
            "illud</rdg><rdg n=\"3\" xml:id=\"rdg1\" resp=\"#Fruterius\">" +
            "illic</rdg><witDetail target=\"#rdg1\" resp=\"#Fruterius\">" +
            "(†1566) 1605a 388</witDetail></app> unde negant redire " +
            "<app n=\"2\"><lem n=\"1\" wit=\"#O #G\">quemquam</lem>" +
            "<rdg n=\"2\" wit=\"#R\">umquam<note>some note</note></rdg></app></p>",
            xml);
    }
}
