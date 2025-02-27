using Xunit;

namespace Cadmus.Export.ML.Test;

public sealed class FrLinkRendererFilterTest
{
    private static RendererContext GetContext()
    {
        RendererContext context = new();
        context.MapSourceId("seg", "it.vedph.token-text:fr.it.vedph.apparatus@0");
        context.MapSourceId("seg", "it.vedph.token-text:fr.it.vedph.apparatus@1");
        return context;
    }

    [Fact]
    public void Apply_NoTags_Unchanged()
    {
        FrLinkRendererFilter filter = new();

        string? result = filter.Apply("hello world", GetContext());

        Assert.NotNull(result);
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void Apply_TagsWithoutMatch_Copied()
    {
        FrLinkRendererFilter filter = new();

        string? result = filter.Apply("hello #[unknown]# world",
            GetContext());

        Assert.NotNull(result);
        Assert.Equal("hello unknown world", result);
    }

    [Fact]
    public void Apply_TagsWithoutMatchWithOmit_Omitted()
    {
        FrLinkRendererFilter filter = new();

        string? result = filter.Apply("hello #[unknown]# world",
            GetContext());

        Assert.NotNull(result);
        Assert.Equal("hello  world", result);
    }

    [Fact]
    public void Apply_TagsWithMatch_Ok()
    {
        FrLinkRendererFilter filter = new();

        string? result = filter.Apply(
            "hello #[seg/it.vedph.token-text:fr.it.vedph.apparatus@0]# world",
            GetContext());

        Assert.NotNull(result);
        Assert.Equal("hello seg1 world", result);
    }
}
