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

public sealed class AppParallelTextTreeFilterTest
{
    private static TokenTextPart GetTextPart()
    {
        TokenTextPart part = new();
        part.Lines.Add(new TextLine
        {
            Y = 1,
            Text = "tecum ludere sicut ipsa possem"
        });
        return part;
    }

    private static TokenTextLayerPart<ApparatusLayerFragment> GetApparatusPart()
    {
        // 1     2      3     4    5
        // tecum ludere sicut ipsa possem
        // AAAAA.BBBBBB............CCCCCC
        TokenTextLayerPart<ApparatusLayerFragment> part = new();

        // tecum OGR: secum O1
        part.Fragments.Add(new()
        {
            Location = "1.1",
            Entries =
            [
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Note,
                    IsAccepted = true,
                    Witnesses =
                    [
                        new AnnotatedValue { Value = "O" },
                        new AnnotatedValue { Value = "G" },
                        new AnnotatedValue { Value = "R" }
                    ]
                },
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "secum",
                    Witnesses =
                    [
                        new AnnotatedValue { Value = "O1" },
                    ]
                }
            ]
        });

        // ludere O1GR: luderem O | loedere Trappers-Lomax, 2007 69
        part.Fragments.Add(new ApparatusLayerFragment()
        {
            Location = "1.2",
            Entries =
            [
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Note,
                    IsAccepted = true,
                    Witnesses =
                    [
                        new AnnotatedValue { Value = "O1" },
                        new AnnotatedValue { Value = "G" },
                        new AnnotatedValue { Value = "R" }
                    ]
                },
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "luderem",
                    Witnesses =
                    [
                        new AnnotatedValue { Value = "O" },
                    ]
                },
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "loedere",
                    Authors =
                    [
                        new LocAnnotatedValue
                        {
                            Value = "Trappers-Lomax",
                            Location= "2007 69"
                        },
                    ]
                }
            ]
        });

        // possem OGR: possum MS48 | possim Turnebus, 1573 26 |
        // posse Vossius, 1684 | posset Heinsius, dub. 1646-81
        part.Fragments.Add(new ApparatusLayerFragment()
        {
            Location = "1.5",
            Entries =
            [
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Note,
                    IsAccepted = true,
                    Witnesses =
                    [
                        new AnnotatedValue { Value = "O" },
                        new AnnotatedValue { Value = "G" },
                        new AnnotatedValue { Value = "R" }
                    ]
                },
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "possum",
                    Witnesses =
                    [
                        new LocAnnotatedValue
                        {
                            Value = "MS48",
                        }
                    ]
                },
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "possim",
                    Authors =
                    [
                        new LocAnnotatedValue
                        {
                            Value = "Turnebus",
                            Location = "1573 26"
                        }
                    ]
                },
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "posse",
                    Authors =
                    [
                        new LocAnnotatedValue
                        {
                            Value = "Vossius",
                            Location = "1684"
                        }
                    ]
                },
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "posset",
                    Authors =
                    [
                        new LocAnnotatedValue
                        {
                            Value = "Heinsius",
                            Note = "dub.",
                            Location = "1646-81"
                        }
                    ]
                }
            ]
        });

        return part;
    }

    public static (TreeNode<TextSpan> tree, IItem item) GetTreeAndItem()
    {
        // get item
        TokenTextPart textPart = GetTextPart();
        TokenTextLayerPart<ApparatusLayerFragment> appPart = GetApparatusPart();
        Item item = new();
        item.Parts.Add(textPart);
        item.Parts.Add(appPart);

        // flatten
        TokenTextPartFlattener flattener = new();
        Tuple<string, IList<AnnotatedTextRange>> tr = flattener.Flatten(
            textPart, [appPart]);

        // merge ranges
        IList<AnnotatedTextRange> mergedRanges = AnnotatedTextRange.MergeRanges(
            0, tr.Item1.Length - 1, tr.Item2);
        // assign text to merged ranges
        foreach (AnnotatedTextRange range in mergedRanges)
            range.AssignText(tr.Item1);

        // build a linear tree from ranges
        TreeNode<TextSpan> tree = ItemComposer.BuildTreeFromRanges(
            mergedRanges, tr.Item1);

        // apply block filter
        return (new BlockLinearTextTreeFilter().Apply(tree, item), item);
    }

    private static void AssertContainsTags(IList<TextSpanFeature> features,
        string context, params string[] tags)
    {
        foreach (string tag in tags)
        {
            bool containsTag = features.Any(f => f.Name == "tag" && f.Value == tag);
            Assert.True(containsTag,
                $"Tag '{tag}' not found in context: {context}");
        }
    }

    [Fact]
    public void Apply_Ok()
    {
        (TreeNode<TextSpan> tree, IItem item) = GetTreeAndItem();
        AppParallelTextTreeFilter filter = new();

        TreeNode<TextSpan> result = filter.Apply(tree, item);

        Assert.NotNull(result);

        // 1.1 root
        Assert.Null(result.Data);

        // 2.1 fork
        Assert.Single(result.Children);
        TreeNode<TextSpan> fork = result.Children[0];
        Assert.Null(fork.Data);

        // 3.1 tecum
        Assert.Equal(2, fork.Children.Count);
        TreeNode<TextSpan>? tecum = fork.FirstChild;
        Assert.NotNull(tecum);
        Assert.Equal("tecum", tecum.Data?.Text);
        // 9 tags: empty, w:O, w:G, w:R,
        // a:Trappers-Lomax, w:MS48, a:Turnebus, a:Vossius, a:Heinsius
        AssertContainsTags(tecum.Data!.Features!, "tecum@3.1",
            "", "w:O", "w:G", "w:R",
            "a:Trappers-Lomax", "w:MS48", "a:Turnebus", "a:Vossius", "a:Heinsius");

        // 4.1 fork
        TreeNode<TextSpan>? fork41 = tecum.FirstChild;
        Assert.NotNull(fork41);
        Assert.NotNull(fork41.Data);
        // 9 tags: empty, w:O, w:G, w:R,
        // a:Trappers-Lomax, w:MS48, a:Turnebus, a:Vossius, a:Heinsius
        AssertContainsTags(fork41.Data.Features!, "fork@4.1",
            "", "w:O", "w:G", "w:R",
            "a:Trappers-Lomax", "w:MS48", "a:Turnebus", "a:Vossius", "a:Heinsius");

        // 5.1. fork
        TreeNode<TextSpan>? fork51 = fork41.FirstChild;
        Assert.NotNull(fork51);
        Assert.Null(fork51.Data);

        // 6.1. ludere
        TreeNode<TextSpan>? ludere = fork51.FirstChild;
        Assert.NotNull(ludere);
        Assert.Equal("ludere", ludere.Data?.Text);
        // 7 tags: empty, w:G, w:R, w:MS48, a:Turnebus, a:Vossius, a:Heinsius
        AssertContainsTags(ludere.Data!.Features!, "ludere@6.1",
            "", "w:G", "w:R", "w:MS48", "a:Turnebus", "a:Vossius", "a:Heinsius");
    }
}
