using Cadmus.Core;
using Cadmus.Export.Renderers;
using Cadmus.Export.Test.Filters;
using Fusi.Tools.Data;
using Xunit;

namespace Cadmus.Export.Test.Renderers;

public sealed class PayloadLinearTextTreeRendererTest
{
    [Fact]
    public void Apply_WithLines_SingleLine_Ok()
    {
        (TreeNode<TextSpanPayload>? tree, IItem _) =
            AppLinearTextTreeFilterTest.GetTreeAndItem();
        PayloadLinearTextTreeRenderer renderer = new();

        string json = renderer.Render(tree, new RendererContext());

        Assert.StartsWith("[[", json);
        Assert.Equal("[[{\"Range\":" +
            "{\"Start\":0,\"End\":4,\"FragmentIds\":" +
            "[\"it.vedph.token-text-layer:fr.it.vedph.apparatus@0\"]," +
            "\"Text\":\"illuc\"},\"Type\":null,\"IsBeforeEol\":false," +
            "\"Text\":\"illuc\",\"FeatureSets\":{}}," +
            "{\"Range\":{\"Start\":5,\"End\":24,\"FragmentIds\":[]," +
            "\"Text\":\" unde negant redire \"},\"Type\":null," +
            "\"IsBeforeEol\":false,\"Text\":\" unde negant redire \"," +
            "\"FeatureSets\":{}}," +
            "{\"Range\":{\"Start\":25,\"End\":32,\"FragmentIds\":" +
            "[\"it.vedph.token-text-layer:fr.it.vedph.apparatus@1\"]," +
            "\"Text\":\"quemquam\"},\"Type\":null,\"IsBeforeEol\":false," +
            "\"Text\":\"quemquam\",\"FeatureSets\":{}}]]", json);
    }

    [Fact]
    public void Apply_WithoutLines_SingleLine_Ok()
    {
        (TreeNode<TextSpanPayload>? tree, IItem _) =
            AppLinearTextTreeFilterTest.GetTreeAndItem();
        PayloadLinearTextTreeRenderer renderer = new();
        renderer.Configure(new PayloadLinearTextTreeRendererOptions
        {
            FlattenLines = true
        });

        string json = renderer.Render(tree, new RendererContext());

        Assert.StartsWith("[{", json);
        Assert.Equal("[{\"Range\":" +
            "{\"Start\":0,\"End\":4,\"FragmentIds\":" +
            "[\"it.vedph.token-text-layer:fr.it.vedph.apparatus@0\"]," +
            "\"Text\":\"illuc\"},\"Type\":null,\"IsBeforeEol\":false," +
            "\"Text\":\"illuc\",\"FeatureSets\":{}}," +
            "{\"Range\":{\"Start\":5,\"End\":24,\"FragmentIds\":[]," +
            "\"Text\":\" unde negant redire \"},\"Type\":null," +
            "\"IsBeforeEol\":false,\"Text\":\" unde negant redire \"," +
            "\"FeatureSets\":{}}," +
            "{\"Range\":{\"Start\":25,\"End\":32,\"FragmentIds\":" +
            "[\"it.vedph.token-text-layer:fr.it.vedph.apparatus@1\"]," +
            "\"Text\":\"quemquam\"},\"Type\":null,\"IsBeforeEol\":false," +
            "\"Text\":\"quemquam\",\"FeatureSets\":{}}]", json);
    }

    // TODO: add multiple lines tests
}
