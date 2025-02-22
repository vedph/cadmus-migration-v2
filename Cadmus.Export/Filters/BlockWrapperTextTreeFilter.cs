using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using System;

namespace Cadmus.Export.Filters;

/// <summary>
/// A text tree filter which wraps each block by inserting an explicit empty
/// text node before each path ending with a text ending with a newline.
/// </summary>
/// <seealso cref="ITextTreeFilter" />
[Tag("text-tree-filter.block-wrapper")]
public sealed class BlockWrapperTextTreeFilter : ITextTreeFilter
{
    public TreeNode<TextSpanPayload> Apply(TreeNode<TextSpanPayload> tree)
    {
        // TODO: implement
        throw new NotImplementedException();
    }
}
