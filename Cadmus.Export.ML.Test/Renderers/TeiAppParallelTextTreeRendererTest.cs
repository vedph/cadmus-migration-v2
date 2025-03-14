using Cadmus.Core;
using Cadmus.Export.Filters;
using Cadmus.Export.ML.Renderers;
using Cadmus.Export.Test.Renderers;
using Fusi.Tools.Data;
using Xunit;

namespace Cadmus.Export.ML.Test.Renderers;

public sealed class TeiAppParallelTextTreeRendererTest
{
    [Fact]
    public void Render_Ok()
    {
        TeiAppParallelTextTreeRenderer renderer = new();
        renderer.Configure(new TeiAppParallelTextTreeRendererOptions
        {
            NoItemSource = true
        });

        // + ⯈ [1.1] #4
        //  + ⯈ [2.1] illuc #1 → illuc
        //   + ⯈ [3.1]  unde negant redire  #2 →  unde negant redire 
        //    - ■ [4.1] quemquam #3 → quemquam
        (TreeNode<TextSpan>? tree, IItem item) =
            PayloadLinearTextTreeRendererTest.GetTreeAndItem();

        // + ⯈ [1.1] #1
        //  + ⯈ [2.1] #2
        //   + ⯈ [3.1] illuc #1 → illuc F2: tag=, tag=w:O1
        //    + ⯈ [4.1]  unde negant redire  #2 →  unde negant redire  F2: tag=, tag=w:O1
        //     - ■ [5.1] quemquam #3 → quemquam F2: tag=, tag=w:O1
        //   + ⯈ [3.2] #4
        //    + ⯈ [4.1] illud #1 → illud F3: tag=w:O, tag=w:G, tag=w:R
        //     + ⯈ [5.1]  unde negant redire  #2 →  unde negant redire  F3: tag=w:O, tag=w:G, tag=w:R
        //      + ⯈ [6.1] #3
        //       - ■ [7.1] quemquam #3 → quemquam F2: tag=w:O, tag=w:G
        //       - ■ [7.2] umquam #3 → umquam F1: tag=w:R
        //    + ⯈ [4.2] illic #1 → illic F1: tag=a:Fruterius
        //     + ⯈ [5.1]  unde negant redire  #2 →  unde negant redire  F1: tag=a:Fruterius
        //      - ■ [6.1] quemquam #3 → quemquam F1: tag=a:Fruterius
        tree = new AppParallelTextTreeFilter().Apply(tree, item);

        // act
        string xml = renderer.Render(tree, new RendererContext
        {
            Item = item
        });

        Assert.Equal("<p n=\"1\" xmlns=\"http://www.tei-c.org/ns/1.0\">TODO</p>",
            xml);
    }

}
