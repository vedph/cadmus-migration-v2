using Cadmus.Core;
using Cadmus.Export.Filters;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Cadmus.Export.Test.Filters;

public sealed class ApparatusLinearTextTreeFilterTest
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

    private static TokenTextLayerPart<ApparatusLayerFragment> GetApparatusPart()
    {
        // 1   2     1     2
        // que bixit|annos XX
        // AAA AAAAA AAAAA
        TokenTextLayerPart<ApparatusLayerFragment> part = new();

        // quae
        part.Fragments.Add(new()
        {
            Location = "1.1",
            Entries =
            [
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "quae",
                    IsAccepted = true,
                    Witnesses =
                    [
                        new AnnotatedValue
                        {
                            Value = "b",
                        }
                    ]
                },
            ]
        });

        // vixit
        part.Fragments.Add(new()
        {
            Location = "1.2",
            Entries =
            [
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "vixit",
                    IsAccepted = true,
                    Witnesses =
                    [
                        new AnnotatedValue
                        {
                            Value = "b",
                            Note = "pc"
                        }
                    ]
                },
            ]
        });

        // annos
        part.Fragments.Add(new()
        {
            Location = "2.1",
            Entries =
            [
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "annis",
                    Authors =
                    [
                        new LocAnnotatedValue
                        {
                            Value = "editor",
                            Note = "accusative here is rare but attested."
                        }
                    ]
                },
            ]
        });

        return part;
    }

    [Fact]
    public void Apply_Ok()
    {
        // get item
        TokenTextPart textPart = GetTextPart();
        TokenTextLayerPart<ApparatusLayerFragment> appPart = GetApparatusPart();
        Item item = new();
        item.Parts.Add(textPart);
        item.Parts.Add(appPart);
        // flatten
        TokenTextPartFlattener flattener = new();
        Tuple<string, IList<FragmentTextRange>> tr = flattener.Flatten(
            textPart, [appPart]);
        // merge ranges
        IList<FragmentTextRange> mergedRanges = FragmentTextRange.MergeRanges(
            0, tr.Item1.Length - 1, tr.Item2);
        // assign text to merged ranges
        foreach (FragmentTextRange range in mergedRanges)
            range.AssignText(tr.Item1);
        // build a linear tree from ranges
        TreeNode<TextSpanPayload> tree = ItemComposer.BuildTreeFromRanges(
            mergedRanges);
        // get filter
        ApparatusLinearTextTreeFilter filter = new();

        filter.Apply(tree, item);

        string prefix = $"{appPart.TypeId}:{appPart.RoleId}_";

        // first node is blank root
        Assert.Null(tree.Data);

        // next child is quae
        Assert.Single(tree.Children);
        TreeNode<TextSpanPayload> node = tree.Children[0];
        Assert.NotNull(node.Data);
        Assert.Equal("quae", node.Data.Text);
        // - app-variant: que, accepted
        Assert.Equal(2, node.Data.Features.Count);
        string id = prefix + "0.0";
        TextSpanFeature? feature = node.Data.Features.FirstOrDefault(
            f => f.Source == id &&
                 f.Name == ApparatusLinearTextTreeFilter.F_APP_VARIANT);
        Assert.NotNull(feature);
        Assert.Equal("que", feature.Value);

        // - witness b
        feature = node.Data.Features.FirstOrDefault(f => f.Source == id &&
            f.Name == ApparatusLinearTextTreeFilter.F_APP_WITNESS);
        Assert.NotNull(feature);
        Assert.Equal("b", feature.Value);

        // next child is space
        Assert.Single(node.Children);
        node = node.Children[0];
        Assert.NotNull(node.Data);
        Assert.Equal(" ", node.Data.Text);
        Assert.Empty(node.Data.Features);

        // next child is vixit
        Assert.Single(node.Children);
        node = node.Children[0];
        Assert.NotNull(node.Data);
        Assert.Equal("vixit", node.Data.Text);
        Assert.Equal(3, node.Data.Features.Count);
        id = prefix + "1.0";

        // - app-variant: vixit|, accepted
        feature = node.Data.Features.FirstOrDefault(f => f.Source == id &&
            f.Name == ApparatusLinearTextTreeFilter.F_APP_VARIANT);
        Assert.NotNull(feature);
        Assert.Equal("bixit\n", feature.Value);

        // - witness b
        feature = node.Data.Features.FirstOrDefault(f => f.Source == id &&
            f.Name == ApparatusLinearTextTreeFilter.F_APP_WITNESS);
        Assert.NotNull(feature);
        Assert.Equal("b", feature.Value);

        // - note pc
        feature = node.Data.Features.FirstOrDefault(f => f.Source == id &&
            f.Name == ApparatusLinearTextTreeFilter.F_APP_WITNESS_NOTE);
        Assert.NotNull(feature);
        Assert.Equal("pc", feature.Value);

        // next child is annos
        Assert.Single(node.Children);
        node = node.Children[0];
        Assert.NotNull(node.Data);
        Assert.Equal("annos", node.Data.Text);
        Assert.Equal(3, node.Data.Features.Count);
        id = prefix + "2.0";

        // -app-variant: annis
        feature = node.Data.Features.FirstOrDefault(f => f.Source == id &&
            f.Name == ApparatusLinearTextTreeFilter.F_APP_VARIANT);
        Assert.NotNull(feature);
        Assert.Equal("annis", feature.Value);

        // -author: editor
        feature = node.Data.Features.FirstOrDefault(f => f.Source == id &&
            f.Name == ApparatusLinearTextTreeFilter.F_APP_AUTHOR);
        Assert.NotNull(feature);
        Assert.Equal("editor", feature.Value);

        // -author-note: accusative here is rare but attested.
        feature = node.Data.Features.FirstOrDefault(f => f.Source == id &&
            f.Name == ApparatusLinearTextTreeFilter.F_APP_AUTHOR_NOTE);
        Assert.NotNull(feature);
        Assert.Equal("accusative here is rare but attested.", feature.Value);

        // next child is " XX"
        Assert.Single(node.Children);
        node = node.Children[0];
        Assert.NotNull(node.Data);
        Assert.Equal(" XX", node.Data.Text);
        Assert.Empty(node.Data.Features);

        // no children
        Assert.False(node.HasChildren);
    }
}
