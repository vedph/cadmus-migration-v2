using Cadmus.Export.Renderers;
using Fusi.Tools.Data;
using Proteus.Core.Text;
using System;
using System.Linq;
using System.Xml.Linq;

namespace Cadmus.Export.ML.Renderers;

/// <summary>
/// Base class for JSON renderers targeting markup languages. This adds
/// more specialized shared logic to <see cref="JsonRenderer"/>.
/// </summary>
/// <seealso cref="JsonRenderer" />
public abstract class MLJsonRenderer : JsonRenderer
{
    /// <summary>
    /// Finds the tree nodes representing the boundaries for the specified
    /// fragment.
    /// </summary>
    /// <param name="prefix">The fragment prefix (typeId:roleId@FrIndex).</param>
    /// <param name="tree">The text tree root node.</param>
    /// <returns>Tuple with first and last node, which might be the same if
    /// the text spans for a single node.</returns>
    public static (TreeNode<TextSpanPayload> First, TreeNode<TextSpanPayload> Last)?
        FindFragmentBounds(string prefix, TreeNode<TextSpanPayload> tree)
    {
        // find the first and last nodes having any fragment ID starting with prefix
        TreeNode<TextSpanPayload>? firstNode = null;
        TreeNode<TextSpanPayload>? lastNode = null;

        tree.Traverse(node =>
        {
            if (node.Data?.Range == null) return true;
            if (node.Data.Range.FragmentIds.Any(s => s.StartsWith(prefix)))
            {
                if (firstNode == null)
                {
                    firstNode = node;
                }
                else
                {
                    lastNode = node;
                    return false;
                }
            }
            return true;
        });

        if (firstNode != null && lastNode == null) return (firstNode, firstNode);

        return lastNode != null ? (firstNode!, lastNode!) : null;
    }

    /// <summary>
    /// Adds the TEI location for a standoff notation to the specified target
    /// element. The location is either encoded as a single @loc attribute or
    /// as a loc child element with @spanFrom and @spanTo attributes.
    /// </summary>
    /// <param name="textPartId">The text part identifier.</param>
    /// <param name="first">The first node linked to the fragment.</param>
    /// <param name="last">The last node linked to the fragment.</param>
    /// <param name="element">The target element to receive location.</param>
    /// <param name="context">The rendering context.</param>
    /// <exception cref="ArgumentNullException">any of the arguments is null
    /// </exception>
    public static void AddTeiLocToElement(string textPartId,
        TreeNode<TextSpanPayload> first,
        TreeNode<TextSpanPayload> last,
        XElement element,
        IRendererContext context)
    {
        ArgumentNullException.ThrowIfNull(textPartId);
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(last);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(context);

        if (first == last)
        {
            int id = context!.MapSourceId("seg", $"{textPartId}_{first}");
            element.SetAttributeValue("loc", $"seg{id}");
        }
        else
        {
            int firstId = context!.MapSourceId("seg", $"{textPartId}_{first}");
            int lastId = context!.MapSourceId("seg", $"{textPartId}_{last}");

            XElement loc = new(NamespaceOptions.TEI + "loc",
                new XAttribute("spanFrom", $"seg{firstId}"),
                new XAttribute("spanTo", $"seg{lastId}"));
            element.Add(loc);
        }
    }
}
