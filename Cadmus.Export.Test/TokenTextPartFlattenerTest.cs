using Cadmus.Core;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using System;
using System.Collections.Generic;
using Xunit;

namespace Cadmus.Export.Test;

public sealed class TokenTextPartFlattenerTest
{
    internal static TokenTextPart GetTextPart(IList<string> lines)
    {
        TokenTextPart part = new();
        int y = 1;
        foreach (string line in lines)
        {
            part.Lines.Add(new TextLine
            {
                Y = y++,
                Text = line
            });
        }
        return part;
    }

    // 123 12345
    // que bixit
    // 12345 12
    // annos XX
    //    0123456789-1234567
    // => que bixit|annos XX
    internal static TokenTextPart GetSampleTextPart()
        => GetTextPart(["que bixit", "annos XX"]);

    internal static IList<IPart> GetSampleLayerParts()
    {
        List<IPart> parts = [];

        // qu[e]
        TokenTextLayerPart<OrthographyLayerFragment>? oLayerPart = new();
        oLayerPart.Fragments.Add(new OrthographyLayerFragment
        {
            Location = "1.1@3"
        });
        // [b]ixit
        oLayerPart.Fragments.Add(new OrthographyLayerFragment
        {
            Location = "1.2@1"
        });
        parts.Add(oLayerPart);

        // qu[e v]ixit
        TokenTextLayerPart<ApparatusLayerFragment>? lLayerPart = new();
        lLayerPart.Fragments.Add(new ApparatusLayerFragment
        {
            Location = "1.1@3-1.2@1"
        });
        parts.Add(lLayerPart);

        // [vixit annos]
        TokenTextLayerPart<CommentLayerFragment>? cLayerPart = new();
        cLayerPart.Fragments.Add(new CommentLayerFragment
        {
            Location = "1.2-2.1"
        });
        parts.Add(cLayerPart);

        return parts;
    }

    [Fact]
    public void GetTextRanges_Ok()
    {
        TokenTextPartFlattener flattener = new();
        TokenTextPart textPart = GetSampleTextPart();
        IList<IPart> layerParts = GetSampleLayerParts();

        Tuple<string, IList<FragmentTextRange>> result = flattener.GetTextRanges(
            textPart, layerParts);

        // text
        // 0123456789-1234567
        // que bixit|annos XX
        // ..O...............
        // ....O.............
        // ..AAA.............
        // ....CCCCCCCCCCC...
        Assert.Equal("que bixit\nannos XX", result.Item1);

        // ranges
        Assert.Equal(4, result.Item2.Count);
        foreach (FragmentTextRange r in result.Item2) r.AssignText(result.Item1);

        // orthography: qu[e]
        FragmentTextRange range = result.Item2[0];
        Assert.Equal("e", range.Text);
        Assert.Equal(2, range.Start);
        Assert.Equal(2, range.End);

        // orthography: [b]ixit
        range = result.Item2[1];
        Assert.Equal("b", range.Text);
        Assert.Equal(4, range.Start);
        Assert.Equal(4, range.End);

        // apparatus: qu[e b]ixit
        range = result.Item2[2];
        Assert.Equal("e b", range.Text);
        Assert.Equal(2, range.Start);
        Assert.Equal(4, range.End);

        // comment: [bixit|annos]
        range = result.Item2[3];
        Assert.Equal("bixit\nannos", range.Text);
        Assert.Equal(4, range.Start);
        Assert.Equal(14, range.End);
    }
}