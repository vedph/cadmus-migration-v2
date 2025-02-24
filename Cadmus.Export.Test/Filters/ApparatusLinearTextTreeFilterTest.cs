using Cadmus.Core;
using Cadmus.Export.Filters;
using Cadmus.General.Parts;
using Cadmus.Philology.Parts;
using Fusi.Tools.Data;
using System;
using System.Collections.Generic;
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
            Text = "illuc unde negant redire quemquam"
        });
        return part;
    }

    private static TokenTextLayerPart<ApparatusLayerFragment> GetApparatusPart()
    {
        // 1     2    3      4      5
        // illuc unde negant redire quemquam
        // AAAAA....................BBBBBBBB
        TokenTextLayerPart<ApparatusLayerFragment> part = new();

        // illuc
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
                        new AnnotatedValue
                        {
                            Value = "O1",
                        }
                    ]
                },
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "illud",
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
                    Value = "illic",
                    Authors =
                    [
                        new LocAnnotatedValue
                        {
                            Value = "Fruterius",
                            Note = "(†1566) 1605a 388"
                        },
                    ]
                },
            ]
        });

        // quemquam
        part.Fragments.Add(new()
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
                    ]
                },
                new ApparatusEntry
                {
                    Type = ApparatusEntryType.Replacement,
                    Value = "umquam",
                    Witnesses =
                    [
                        new AnnotatedValue { Value = "R" }
                    ],
                    Note = "some note"
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
            mergedRanges, tr.Item1);
        // apply block filter
        tree = new BlockLinearTextTreeFilter().Apply(tree, item);

        // act
        ApparatusLinearTextTreeFilter filter = new();
        filter.Apply(tree, item);

        // assert
        string prefix = $"{appPart.TypeId}:{appPart.RoleId}@";

        // first node is blank root
        Assert.Null(tree.Data);

        // next child is illuc
        Assert.Single(tree.Children);
        TreeNode<TextSpanPayload> node = tree.Children[0];
        Assert.NotNull(node.Data);
        Assert.Equal("illuc", node.Data.Text);

        // illuc has 8 features
        Assert.Equal(8, node.Data.Features.Count);
        List<Tuple<FragmentFeatureSource, TextSpanFeature>> feats =
            node.Data.GetFragmentFeatures(prefix);

        // from entry 0: app-witness=O1
        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_WITNESS,
            feats[0].Item2.Name);
        Assert.Equal("O1", feats[0].Item2.Value);

        // from entry 1:
        // - app-variant=illud
        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_VARIANT,
            feats[1].Item2.Name);
        Assert.Equal("illud", feats[1].Item2.Value);

        // - app-witness=O,G,R
        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_WITNESS,
            feats[2].Item2.Name);
        Assert.Equal("O", feats[2].Item2.Value);

        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_WITNESS,
            feats[3].Item2.Name);
        Assert.Equal("G", feats[3].Item2.Value);

        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_WITNESS,
            feats[4].Item2.Name);
        Assert.Equal("R", feats[4].Item2.Value);

        // from entry 2:
        // - app-variant=illic
        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_VARIANT,
            feats[5].Item2.Name);
        Assert.Equal("illic", feats[5].Item2.Value);

        // - app-author=Fruterius
        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_AUTHOR,
            feats[6].Item2.Name);
        Assert.Equal("Fruterius", feats[6].Item2.Value);

        // - app-author.note=(†1566) 1605a 388
        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_AUTHOR_NOTE,
            feats[7].Item2.Name);
        Assert.Equal("(†1566) 1605a 388", feats[7].Item2.Value);

        // next child is unde negant redire
        Assert.Single(node.Children);
        node = node.Children[0];
        Assert.NotNull(node.Data);
        Assert.Equal(" unde negant redire ", node.Data.Text);
        Assert.Empty(node.Data!.Features);

        // next child is quemquam
        Assert.Single(node.Children);
        node = node.Children[0];
        Assert.NotNull(node.Data);
        Assert.Equal("quemquam", node.Data.Text);

        // quemquam has 5 features
        Assert.Equal(5, node.Data.Features.Count);
        feats = node.Data.GetFragmentFeatures(prefix);

        // from entry 0:
        // - app-witness=O,G
        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_WITNESS,
            feats[0].Item2.Name);
        Assert.Equal("O", feats[0].Item2.Value);

        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_WITNESS,
            feats[1].Item2.Name);
        Assert.Equal("G", feats[1].Item2.Value);

        // from entry 1:
        // - app-variant=umquam
        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_VARIANT,
            feats[2].Item2.Name);
        Assert.Equal("umquam", feats[2].Item2.Value);

        // - app-witness=R
        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_WITNESS,
            feats[3].Item2.Name);
        Assert.Equal("R", feats[3].Item2.Value);

        // - app-note=some note
        Assert.Equal(ApparatusLinearTextTreeFilter.F_APP_NOTE,
            feats[4].Item2.Name);
        Assert.Equal("some note", feats[4].Item2.Value);

        // no more children
        Assert.Empty(node.Children);
    }
}
