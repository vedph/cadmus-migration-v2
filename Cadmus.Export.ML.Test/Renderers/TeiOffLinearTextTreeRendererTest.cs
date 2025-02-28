﻿using Cadmus.Core;
using Cadmus.Epigraphy.Parts;
using Cadmus.Export.Filters;
using Cadmus.Export.ML.Renderers;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Data;
using System;
using System.Collections.Generic;
using Xunit;

namespace Cadmus.Export.ML.Test.Renderers;

public sealed class TeiOffLinearTextTreeRendererTest
{
    private static TokenTextPart GetTextPart()
    {
        TokenTextPart part = new();

        part.Lines.Add(new TextLine
        {
            Y = 1,
            Text = "que bixit"
        });
        part.Lines.Add(new TextLine
        {
            Y = 2,
            Text = "annos XX"
        });

        return part;
    }

    private static IPart[] GetLayerParts()
    {
        // 012345678901234567
        // que bixit|annos XX
        // ..O...............
        // ....O.............
        // ..PPP.............
        // ....CCCCCCCCCCC...

        // que
        TokenTextLayerPart<OrthographyLayerFragment> orthLayerPart = new();
        orthLayerPart.Fragments.Add(new OrthographyLayerFragment()
        {
            Location = "1.1@3",
            Standard = "quae"
        });

        // bixit
        orthLayerPart.Fragments.Add(new OrthographyLayerFragment()
        {
            Location = "1.2@1",
            Standard = "vixit"
        });

        // e-b
        TokenTextLayerPart<EpiLigaturesLayerFragment> ligLayerPart = new();
        ligLayerPart.Fragments.Add(new EpiLigaturesLayerFragment
        {
            Location = "1.1@3-1.2@1",
            Types = new HashSet<string>(["connection"])
        });

        // bixit annos
        TokenTextLayerPart<CommentLayerFragment> commentLayerPart = new();
        commentLayerPart.Fragments.Add(new CommentLayerFragment
        {
            Location = "1.2-1.3",
            Tag = "syntax",
            Text = "accusative rather than ablative is rare but attested."
        });

        return
        [
            orthLayerPart,
            ligLayerPart,
            commentLayerPart
        ];
    }

    public static (TreeNode<TextSpanPayload> tree, IItem item) GetTreeAndItem()
    {
        // get item
        TokenTextPart textPart = GetTextPart();
        Item item = new();
        item.Parts.Add(textPart);
        IPart[] layerParts = GetLayerParts();
        item.Parts.AddRange(layerParts);

        // flatten
        TokenTextPartFlattener flattener = new();
        Tuple<string, IList<FragmentTextRange>> tr = flattener.Flatten(
            textPart, layerParts);

        // merge ranges
        IList<FragmentTextRange> mergedRanges = FragmentTextRange.MergeRanges(
            0, tr.Item1.Length - 1, tr.Item2);
        // assign text to merged ranges
        foreach (FragmentTextRange range in mergedRanges)
            range.AssignText(tr.Item1);

        // build a linear tree from ranges
        TreeNode<TextSpanPayload> tree = ItemComposer.BuildTreeFromRanges(
            mergedRanges, tr.Item1);
        // apply block filter
        return (new BlockLinearTextTreeFilter().Apply(tree, item), item);
    }

    [Fact]
    public void Render_BaseText_Ok()
    {
        TeiOffLinearTextTreeRenderer renderer = new();

        (TreeNode<TextSpanPayload>? tree, IItem item) = GetTreeAndItem();

        // ensure that tree is as expected:
        // root
        //  - qu
        //    - e
        //      - (space)
        //        - b
        //          - ixit with LF marker
        //            - annos
        //              - space + XX
        Assert.Null(tree.Data);
        TreeNode<TextSpanPayload> qu = tree.Children[0];
        Assert.NotNull(qu.Data);
        Assert.Equal("qu", qu.Data.Text);
        TreeNode<TextSpanPayload> e = qu.Children[0];
        Assert.NotNull(e.Data);
        Assert.Equal("e", e.Data.Text);
        TreeNode<TextSpanPayload> space = e.Children[0];
        Assert.NotNull(space.Data);
        Assert.Equal(" ", space.Data.Text);
        TreeNode<TextSpanPayload> b = space.Children[0];
        Assert.NotNull(b.Data);
        Assert.Equal("b", b.Data.Text);
        TreeNode<TextSpanPayload> ixit = b.Children[0];
        Assert.NotNull(ixit.Data);
        Assert.Equal("ixit", ixit.Data.Text);
        Assert.True(ixit.Data.IsBeforeEol);
        TreeNode<TextSpanPayload> annos = ixit.Children[0];
        Assert.NotNull(annos.Data);
        Assert.Equal("annos", annos.Data.Text);
        TreeNode<TextSpanPayload> xx = annos.Children[0];
        Assert.NotNull(xx.Data);
        Assert.Equal(" XX", xx.Data.Text);

        // act
        string xml = renderer.Render(tree, new RendererContext
        {
            Item = item
        });

        // assert
        Assert.Equal(
            "<p n=\"1\" xmlns=\"http://www.tei-c.org/ns/1.0\">" +
            "<seg xml:id=\"seg1\">qu</seg>" +
            "<seg xml:id=\"seg2\">e</seg>" +
            "<seg xml:id=\"seg3\"> </seg>" +
            "<seg xml:id=\"seg4\">b</seg>" +
            "<seg xml:id=\"seg5\">ixit</seg></p>" +
            "<p n=\"2\" xmlns=\"http://www.tei-c.org/ns/1.0\">" +
            "<seg xml:id=\"seg6\">annos</seg>" +
            " XX</p>", xml);
    }
}
