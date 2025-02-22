using System.Collections.Generic;
using Cadmus.Export.Filters;
using Xunit;

namespace Cadmus.Export.Test.Filters;

public sealed class ReplaceRendererFilterTest
{
    private static IRendererFilter GetFilter()
    {
        ReplaceRendererFilter filter = new();
        filter.Configure(new ReplaceRendererFilterOptions
        {
            Replacements = new List<ReplaceEntryOptions>
            {
                new ReplaceEntryOptions
                {
                    Source = "hello",
                    Target = "HELLO"
                }
            }
        });
        return filter;
    }

    [Fact]
    public void Apply_NoMatch_Unchanged()
    {
        IRendererFilter filter = GetFilter();

        string result = filter.Apply("Goodbye, world!");

        Assert.Equal("Goodbye, world!", result);
    }

    [Fact]
    public void Apply_MatchLiteral_Ok()
    {
        IRendererFilter filter = GetFilter();

        string result = filter.Apply("hello, world!");

        Assert.Equal("HELLO, world!", result);
    }

    [Fact]
    public void Apply_MatchPattern_Ok()
    {
        ReplaceRendererFilter filter = new();
        filter.Configure(new ReplaceRendererFilterOptions
        {
            Replacements = new List<ReplaceEntryOptions>
            {
                new ReplaceEntryOptions
                {
                    Source = "[Hh]ello",
                    Target = "HELLO",
                    IsPattern = true
                }
            }
        });

        string result = filter.Apply("Hello, world!");

        Assert.Equal("HELLO, world!", result);
    }
}
