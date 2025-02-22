using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using Fusi.Tools.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cadmus.Export.ML;

/// <summary>
/// Standoff TEI text tree renderer.
/// <para>Tag: <c>it.vedph.text-block-renderer.tei-standoff</c>.</para>
/// </summary>
[Tag("it.vedph.text-block-renderer.tei-standoff")]
public sealed class TeiStandoffTextTreeRenderer : TextTreeRenderer,
    ITextTreeRenderer,
    IConfigurable<TeiStandoffTextBlockRendererOptions>
{
    /// <summary>
    /// The name of the metadata placeholder for the item's ordinal number
    /// (1-N). This is set externally when repeatedly using this renderer
    /// for multiple items.
    /// </summary>
    public const string M_ITEM_NR = "item-nr";
    /// <summary>
    /// The name of the metadata placeholder for each block's target ID.
    /// Each target ID is built with item number + layer ID + block number,
    /// all separated by underscore and prefixed by an initial single <c>b</c>
    /// (e.g. <c>b1_2_3</c>).
    /// </summary>
    public const string M_TARGET_ID = "target-id";
    /// <summary>
    /// The name of the metadata placeholder for row's y number (1-N).
    /// </summary>
    public const string M_ROW_Y = "y";
    /// <summary>
    /// The name of the metadata placeholder for block's ID.
    /// </summary>
    public const string M_BLOCK_ID = "b";

    private readonly Dictionary<string, object> _nullCtxData;
    private TeiStandoffTextBlockRendererOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeiStandoffTextTreeRenderer"/>
    /// class.
    /// </summary>
    public TeiStandoffTextTreeRenderer()
    {
        _nullCtxData = [];
        _options = new TeiStandoffTextBlockRendererOptions();
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(TeiStandoffTextBlockRendererOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    private static string Xmlize(string text)
    {
        if (!text.Contains('<') &&
            !text.Contains('>') &&
            !text.Contains('&'))
        {
            return text;
        }

        StringBuilder sb = new(text);
        sb.Replace("&", "&amp;");
        sb.Replace("<", "&lt;");
        sb.Replace(">", "&gt;");
        return sb.ToString();
    }

    //private static string GetLayerIdPrefix(string id)
    //{
    //    int i = id.Length;
    //    while (i > 0 && (id[i - 1] >= '0' && id[i - 1] <= '9')) i--;
    //    return id[0..i];
    //}

    private void RenderRowText(int y, TextBlockRow row, StringBuilder text,
        IRendererContext context)
    {
        // open row
        if (!string.IsNullOrEmpty(_options.RowOpen))
        {
            text.Append(TextTemplate.FillTemplate(_options.RowOpen,
                context?.Data ?? _nullCtxData));
        }

        // for each block in row
        foreach (TextBlock block in row.Blocks)
        {
            // target block ID
            string targetId = $"{context!.Data[M_ITEM_NR]}_{y}_{block.Id}";
            context.Data[M_TARGET_ID] = targetId;
            context.Data[M_BLOCK_ID] = block.Id;

            // is there any layer linked to this block?
            if (block.LayerIds.Count > 0)
            {
                // collect fragment IDs
                foreach (string id in block.LayerIds)
                    context.FragmentIds[id] = targetId;

                // open block
                if (!string.IsNullOrEmpty(_options.BlockOpen))
                {
                    text.Append(TextTemplate.FillTemplate(_options.BlockOpen,
                        context?.Data ?? _nullCtxData));
                }

                text.Append(Xmlize(block.Text));

                // close block
                if (!string.IsNullOrEmpty(_options.BlockClose))
                {
                    text.Append(TextTemplate.FillTemplate(_options.BlockClose,
                        context?.Data ?? _nullCtxData));
                }
            }
            // no linked layer: just append text
            else
            {
                text.Append(Xmlize(block.Text));
            }
        }

        // close row
        if (!string.IsNullOrEmpty(_options.RowClose))
        {
            text.Append(TextTemplate.FillTemplate(_options.RowClose,
                context?.Data ?? _nullCtxData));
        }
    }

    /// <summary>
    /// Renders the specified rows.
    /// </summary>
    /// <param name="tree">The root node of the text tree.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>Rendition.</returns>
    /// <exception cref="ArgumentNullException">rows</exception>
    protected override string DoRender(TreeNode<TextSpanPayload> tree,
        IRendererContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(tree);

        // render each row of blocks
        StringBuilder text = new();
        int y = 0;
        // TODO: implement
        //foreach (TextBlockRow row in rows)
        //{
        //    if (context != null) context.Data[M_ROW_Y] = ++y;
        //    RenderRowText(y, row, text, context!);
        //}

        return text.ToString();
    }
}

#region TeiStandoffTextBlockRendererOptions
/// <summary>
/// Options for <see cref="TeiStandoffTextTreeRenderer"/>.
/// </summary>
public class TeiStandoffTextBlockRendererOptions
{
    /// <summary>
    /// Gets or sets the code to insert at each row start.
    /// This can be a template, with placeholders delimited by curly braces.
    /// </summary>
    public string? RowOpen { get; set; }

    /// <summary>
    /// Gets or sets the code to insert at each row end.
    /// This can be a template, with placeholders delimited by curly braces.
    /// </summary>
    public string? RowClose { get; set; }

    /// <summary>
    /// Gets or sets the code to insert at each block start.
    /// This can be a template, with placeholders delimited by curly braces.
    /// </summary>
    public string? BlockOpen { get; set; }

    /// <summary>
    /// Gets or sets the code to insert at each block end.
    /// This can be a template, with placeholders delimited by curly braces.
    /// </summary>
    public string? BlockClose { get; set; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="TeiStandoffTextBlockRendererOptions"/> class.
    /// </summary>
    public TeiStandoffTextBlockRendererOptions()
    {
        RowOpen = "<div xml:id=\"r{" +
            TeiStandoffTextTreeRenderer.M_ITEM_NR + "}_{" +
            TeiStandoffTextTreeRenderer.M_ROW_Y + "}\">";
        RowClose = "</div>";
        BlockOpen = "<seg xml:id=\"{" +
            TeiStandoffTextTreeRenderer.M_TARGET_ID + "}\">";
        BlockClose = "</seg>";
    }
}
#endregion
