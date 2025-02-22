using Fusi.Tools.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadmus.Export;

/// <summary>
/// Builder of <see cref="TextBlock"/>'s. This gets a text with its merged
/// ranges set, and builds a set of lists of text blocks, one for each
/// original line in the text. Every block encodes the span of text linked
/// to the same subset of layer IDs. Further processing can then add
/// decoration and tip to blocks.
/// </summary>
public class TextBlockBuilder
{
    /// <summary>
    /// Gets or sets the separator used to separate rows in the source text.
    /// The default value is LF.
    /// </summary>
    public string Separator { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBlockBuilder"/> class.
    /// </summary>
    public TextBlockBuilder()
    {
        Separator = "\n";
    }

    private bool HasSeparatorAt(string text, int index)
    {
        if (Separator.Length + index > text.Length) return false;
        for (int i = 0; i < Separator.Length; i++)
        {
            if (text[i + index] != Separator[i]) return false;
        }
        return true;
    }

    /// <summary>
    /// Builds rows of text blocks from the specified text and ranges set.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="set">The ranges set.</param>
    /// <returns>Enumerable of list of text blocks, each representing a row
    /// (line) in the original text.</returns>
    /// <exception cref="ArgumentNullException">text or set</exception>
    public IEnumerable<TextBlockRow> Build(string text,
        MergedRangeSet set)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(set);

        int i = 1, n = 0, start = 0;
        TextBlockRow row = new();

        while (i < text.Length)
        {
            // whenever we find a line delimiter, end the row of blocks
            // and emit it, then continue with a new row
            if (HasSeparatorAt(text, i))
            {
                if (i > start)
                {
                    IList<MergedRange> ranges = set.GetRangesAt(start);
                    row.Blocks.Add(new TextBlock(
                        $"{++n}", text[start..i], ranges.Select(r => r.Id!)));
                }
                if (row.Blocks.Count > 0) yield return row;

                row = new TextBlockRow();
                i += Separator.Length;
                start = i;
                continue;
            }

            if (!set.AreEqualAt(i, i - 1))
            {
                if (i > start)
                {
                    IList<MergedRange> ranges = set.GetRangesAt(start);
                    row.Blocks.Add(new TextBlock(
                        $"{++n}", text[start..i], ranges.Select(r => r.Id!)));
                }
                start = i++;
            }
            else i++;
        }

        if (i > start)
        {
            IList<MergedRange> ranges = set.GetRangesAt(start);
            row.Blocks.Add(new TextBlock(
                $"{n + 1}", text[start..i], ranges.Select(r => r.Id!)));
        }
        if (row.Blocks.Count > 0) yield return row;
    }
}
