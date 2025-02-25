using Cadmus.Core;
using Cadmus.Export.Suppliers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Cadmus.Export.Test.Suppliers;

public sealed class FlagRendererContextSupplierTest
{
    [Fact]
    public void GetFlagRendererContext_UnmappedFlags_DoesNotSupplyPairs()
    {
        // TODO
    }

    [Fact]
    public void GetFlagRendererContext_MappedFlags_SuppliesPairs()
    {
        RendererContext context = new()
        {
            Item = new Item()
            {
                Flags = 0x0001 | 0x0004 | 0x0010
            }
        };
    }

}
