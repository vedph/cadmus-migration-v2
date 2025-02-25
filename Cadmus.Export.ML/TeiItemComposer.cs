using Cadmus.Core;
using Fusi.Tools.Data;
using System.Xml.Linq;

namespace Cadmus.Export.ML;

/// <summary>
/// Simple TEI item composer.
/// </summary>
/// <seealso cref="ItemComposer" />
public abstract class TeiItemComposer : ItemComposer
{
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
    /// Composes the output from the specified item.
    /// </summary>
    /// <returns>Composition result or null.</returns>
    protected override void DoCompose()
    {
        if (Output == null || TextTreeRenderer == null || Context.Item == null)
            return;

        // build text tree
        TreeNode<TextSpanPayload>? tree = BuildTextTree(Context.Item);
        if (tree == null) return;

        // render text from tree
        string result = TextTreeRenderer.Render(tree, Context);
        WriteOutput(PartBase.BASE_TEXT_ROLE_ID, result);
    }
}
