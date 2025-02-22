using System.Collections.Generic;

namespace Cadmus.Export;

/// <summary>
/// A row of <see cref="TextBlock"/>'s.
/// </summary>
public class TextBlockRow
{
    /// <summary>
    /// Gets the blocks in this row.
    /// </summary>
    public IList<TextBlock> Blocks { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBlockRow"/> class.
    /// </summary>
    public TextBlockRow()
    {
        Blocks = new List<TextBlock>();
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Blocks.Count}: {(Blocks.Count > 0 ? Blocks[0].Text : "-")}";
    }
}
