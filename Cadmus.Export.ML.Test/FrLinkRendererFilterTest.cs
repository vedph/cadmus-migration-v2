using Xunit;

namespace Cadmus.Export.ML.Test;

public sealed class FrLinkRendererFilterTest
{
    private static IRendererContext GetContext()
    {
        RendererContext context = new();
        context.FragmentIds["typex|roley0"] = "1_2_3";
        context.FragmentIds["typez1"] = "2_4_6";
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
    public void Apply_TagsWithoutMatch_Ok()
    {
        FrLinkRendererFilter filter = new();

        string? result = filter.Apply("hello #[typeunknown1]# world",
            GetContext());

        Assert.NotNull(result);
        Assert.Equal("hello typeunknown1 world", result);
    }

    [Fact]
    public void Apply_TagsWithMatch_Ok()
    {
        FrLinkRendererFilter filter = new();

        string? result = filter.Apply("hello #[typex|roley0]# world",
            GetContext());

        Assert.NotNull(result);
        Assert.Equal("hello 1_2_3 world", result);
    }
}
