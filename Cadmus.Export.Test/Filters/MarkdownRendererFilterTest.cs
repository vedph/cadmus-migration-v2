using Cadmus.Export.Filters;
using Xunit;

namespace Cadmus.Export.Test.Filters;

public sealed class MarkdownRendererFilterTest
{
    private static MarkdownRendererFilter GetFilter()
    {
        MarkdownRendererFilter filter = new();
        filter.Configure(new MarkdownRendererFilterOptions
        {
            Format = "html",
            MarkdownOpen = "<_md>",
            MarkdownClose = "</_md>"
        });
        return filter;
    }

    [Fact]
    public void Apply_NoRegion_Unchanged()
    {
        MarkdownRendererFilter filter = GetFilter();

        string result = filter.Apply("No markdown here");

        Assert.Equal("No markdown here", result);
    }

    [Fact]
    public void Apply_Regions_Ok()
    {
        MarkdownRendererFilter filter = GetFilter();

        string result = filter.Apply("Hello. <_md>This *is* MD.</_md> End.");

        Assert.Equal("Hello. <p>This <em>is</em> MD.</p>\n End.", result);
    }

    [Fact]
    public void Apply_WholeText_Ok()
    {
        MarkdownRendererFilter filter = new();
        filter.Configure(new MarkdownRendererFilterOptions
        {
            Format = "html"
        });

        string result = filter.Apply("This *is* MD.");

        Assert.Equal("<p>This <em>is</em> MD.</p>\n", result);
    }
}
