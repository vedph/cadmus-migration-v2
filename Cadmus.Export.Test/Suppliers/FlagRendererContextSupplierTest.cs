using Cadmus.Core;
using Cadmus.Export.Suppliers;
using System.Collections.Generic;
using Xunit;

namespace Cadmus.Export.Test.Suppliers;

public sealed class FlagRendererContextSupplierTest
{
    [Fact]
    public void GetFlagRendererContext_MappedFlags_SuppliesPairs()
    {
        RendererContext context = new()
        {
            Item = new Item()
            {
                Flags = 0x0001 | 0x0004
            }
        };
        FlagRendererContextSupplier supplier = new();
        supplier.Configure(new FlagRendererContextSupplierOptions
        {
            Mappings = new Dictionary<string, string>()
            {
                ["1"] = "alpha=one",
                ["4"] = "beta=four",
                ["h10"] = "gamma=sixteen",
            }
        });

        supplier.Supply(context);

        // gamma not present
        Assert.False(context.Data.ContainsKey("gamma"));

        // alpha=one
        Assert.True(context.Data.ContainsKey("alpha"));
        Assert.Equal("one", context.Data["alpha"]);

        // beta=four
        Assert.True(context.Data.ContainsKey("beta"));
        Assert.Equal("four", context.Data["beta"]);
    }
}
