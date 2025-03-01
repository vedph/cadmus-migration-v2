using Cadmus.Export.ML.Filters;
using Xunit;

namespace Cadmus.Export.ML.Test.Filters;

public sealed class FrLinkRendererFilterTest
{
    private static RendererContext GetContext()
    {
        RendererContext context = new();
        context.MapSourceId("seg",
            "db66b931-d468-4478-a6ae-d9e56e9431b9/0");
        context.MapSourceId("seg",
            "db66b931-d468-4478-a6ae-d9e56e9431b9/1");
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
    public void Apply_TagsWithoutMatch_Unresolved()
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
        filter.Configure(new FrLinkRendererFilterOptions
        {
            OmitUnresolved = true
        });

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
            "hello #[seg/db66b931-d468-4478-a6ae-d9e56e9431b9/0]# world",
            GetContext());

        Assert.NotNull(result);
        Assert.Equal("hello seg1 world", result);
    }
}
