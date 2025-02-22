﻿using Cadmus.Core;
using Fusi.Tools.Data;
using System;
using System.Text.Json;
using System.Xml.Linq;

namespace Cadmus.Export.ML;

/// <summary>
/// Base class for TEI standoff item composers. This deals with text items,
/// using an <see cref="ITextPartFlattener"/> to flatten it with all its
/// layers, and an <see cref="ITextTreeRenderer"/> to render the resulting
/// text blocks into XML. It then uses a number of <see cref="IJsonRenderer"/>'s
/// to render each layer's fragment in its own XML document. So, ultimately
/// this produces several XML documents, one for the base text and as many
/// documents as its layers.
/// </summary>
/// <seealso cref="ItemComposer" />
public abstract class TeiStandoffItemComposer : ItemComposer
{
    private readonly JsonSerializerOptions _jsonOptions;
    private int _nextLayerId;

    /// <summary>
    /// The TEI namespace.
    /// </summary>
    public readonly XNamespace TEI_NS = "http://www.tei-c.org/ns/1.0";

    /// <summary>
    /// The text flow metadata key (<c>flow-key</c>).
    /// </summary>
    public const string M_FLOW_KEY = "flow-key";

    /// <summary>
    /// The layer identifier (<c>layer-id</c>). This is from the renderer
    /// context layer ID mappings.
    /// </summary>
    public const string M_LAYER_ID = "layer-id";

    /// <summary>
    /// Initializes a new instance of the <see cref="TeiStandoffItemComposer"/>
    /// class.
    /// </summary>
    protected TeiStandoffItemComposer()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    private string BuildLayerIdPrefix(IPart part)
    {
        string id = part.TypeId;
        if (!string.IsNullOrEmpty(part.RoleId)) id += "|" + part.RoleId;

        // save layer ID (1-N)
        if (!Context.LayerIds.ContainsKey(id))
            Context.LayerIds[id] = ++_nextLayerId;

        return id;
    }

    /// <summary>
    /// Composes the output from the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>Composition result or null.</returns>
    /// <exception cref="ArgumentNullException">item</exception>
    protected override void DoCompose(IItem item)
    {
        if (Output == null || TextTreeRenderer == null) return;

        // build text tree
        TreeNode<TextSpanPayload>? tree = BuildTextTree(item);
        if (tree == null) return;

        // render text from tree
        string result = TextTreeRenderer.Render(tree, Context);
        WriteOutput(PartBase.BASE_TEXT_ROLE_ID, result);

        // render layers
        foreach (IPart layerPart in GetLayerParts(item))
        {
            string id = BuildLayerIdPrefix(layerPart);

            if (JsonRenderers.TryGetValue(id, out IJsonRenderer? value))
            {
                Context.Data[M_LAYER_ID] = Context.LayerIds[id];
                string json = JsonSerializer.Serialize<object>(layerPart,
                    _jsonOptions);
                result = value.Render(json, Context);
                WriteOutput(id, result);
            }
        }
    }
}

#region TeiStandoffItemComposerOptions
/// <summary>
/// Base options for TEI standoff item composers.
/// </summary>
public class TeiStandoffItemComposerOptions
{
    /// <summary>
    /// Gets or sets the optional text head. This is written at the start
    /// of the text flow. Its value can include placeholders in curly
    /// braces, corresponding to any of the metadata keys defined in
    /// the item composer's context.
    /// </summary>
    public string? TextHead { get; set; }

    /// <summary>
    /// Gets or sets the optional text tail. This is written at the end
    /// of the text flow. Its value can include placeholders in curly
    /// braces, corresponding to any of the metadata keys defined in
    /// the item composer's context.
    /// </summary>
    public string? TextTail { get; set; }

    /// <summary>
    /// Gets or sets the optional layer head. This is written at the start
    /// of each layer flow. Its value can include placeholders in curly
    /// braces, corresponding to any of the metadata keys defined in
    /// the item composer's context.
    /// </summary>
    public string? LayerHead { get; set; }

    /// <summary>
    /// Gets or sets the optional layer tail. This is written at the end
    /// of each layer flow. Its value can include placeholders in curly
    /// braces, corresponding to any of the metadata keys defined in
    /// the item composer's context.
    /// </summary>
    public string? LayerTail { get; set; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="TeiStandoffItemComposerOptions"/> class.
    /// </summary>
    public TeiStandoffItemComposerOptions()
    {
        TextHead = "<body>";
        TextTail = "</body>";
        LayerHead = "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
            Environment.NewLine +
            "<standOff type=\"{" +
            ItemComposer.M_ITEM_NR + "}\">";
        LayerTail = "</standOff>" + Environment.NewLine + "</TEI>";
    }
}
#endregion
