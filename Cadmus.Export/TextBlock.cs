using System;
using System.Collections.Generic;

namespace Cadmus.Export;

/// <summary>
/// A block of text linked to any number of layer IDs. This is the model
/// consumed by frontend tools in the Cadmus bricks library
/// (see https://github.com/vedph/cadmus-bricks-shell/tree/master/projects/myrmidon/cadmus-text-block-view).
/// </summary>
public class TextBlock
{
    /// <summary>
    /// Gets or sets the block's identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the block's text.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the optional decoration for this block. When
    /// <see cref="HtmlDecoration"/> is true, this contains HTML code
    /// (e.g. some SVG); otherwise it's just plain text.
    /// </summary>
    public string? Decoration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Decoration"/>
    /// is HTML rather than plain text.
    /// </summary>
    public bool HtmlDecoration { get; set; }

    /// <summary>
    /// Gets or sets the tip.
    /// </summary>
    public string? Tip { get; set; }

    /// <summary>
    /// Gets the IDs of the layers linked to this block.
    /// </summary>
    public IList<string> LayerIds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBlock"/> class.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="text">The text.</param>
    /// <param name="layerIds">The optional layer IDs.</param>
    /// <exception cref="ArgumentNullException">id or text</exception>
    public TextBlock(string id, string text,
        IEnumerable<string>? layerIds = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Text = text ?? throw new ArgumentNullException(nameof(text));
        LayerIds = new List<string>(layerIds ?? Array.Empty<string>());
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"#{Id}: {Text} ({LayerIds.Count})";
    }
}
