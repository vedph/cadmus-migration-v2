using Cadmus.Core;
using Cadmus.Export.Filters;
using Fusi.Tools.Data;
using Xunit;

namespace Cadmus.Export.Test.Filters;

public class BlockLinearTextTreeFilterTests
{
    private static TreeNode<TextSpanPayload> CreateNode(string text)
    {
        return new TreeNode<TextSpanPayload>(
            new TextSpanPayload(
                new FragmentTextRange(0, text.Length))
                {
                    Text = text
                });
    }

    [Fact]
    public void Apply_SingleNodeNoNewline_ReturnsSameNode()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpanPayload> root = CreateNode("Hello world");

        TreeNode<TextSpanPayload> result = filter.Apply(root, new Item());

        Assert.Single(result.Children);
        Assert.Equal("Hello world", result.Children[0].Data!.Text);

        Assert.Empty(result.Children[0].Children);
    }

    [Fact]
    public void Apply_SingleNodeWithNewline_SplitsNode()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpanPayload> root = CreateNode("Hello\nworld");

        TreeNode<TextSpanPayload> result = filter.Apply(root, new Item());

        // Hello\n
        Assert.Single(result.Children);
        TreeNode<TextSpanPayload> left = result.Children[0];
        Assert.Equal("Hello", left.Data!.Text);
        Assert.True(left.Data.IsBeforeEol);

        // world
        Assert.Single(left.Children);
        TreeNode<TextSpanPayload> right = left.Children[0];
        Assert.Equal("world", right.Data!.Text);
        Assert.False(right.Data.IsBeforeEol);

        Assert.Empty(right.Children);
    }

    [Fact]
    public void Apply_MultipleNewlines_SplitsNodes()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpanPayload> root = CreateNode("Hello\nworld\nagain");

        TreeNode<TextSpanPayload> result = filter.Apply(root, new Item());

        // Hello\n
        Assert.Single(result.Children);
        TreeNode<TextSpanPayload> left = result.Children[0];
        Assert.Equal("Hello", left.Data!.Text);
        Assert.True(left.Data.IsBeforeEol);

        // world\n
        Assert.Single(left.Children);
        TreeNode<TextSpanPayload> middle = left.Children[0];
        Assert.Equal("world", middle.Data!.Text);
        Assert.True(middle.Data.IsBeforeEol);

        // again
        Assert.Single(middle.Children);
        TreeNode<TextSpanPayload> right = middle.Children[0];
        Assert.Equal("again", right.Data!.Text);
        Assert.False(right.Data.IsBeforeEol);

        Assert.Empty(right.Children);
    }

    [Fact]
    public void Apply_EndsWithNewline_SplitsNodes()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpanPayload> root = CreateNode("Hello\nworld\n");

        TreeNode<TextSpanPayload> result = filter.Apply(root, new Item());

        // Hello\n
        Assert.Single(result.Children);
        TreeNode<TextSpanPayload> left = result.Children[0];
        Assert.Equal("Hello", left.Data!.Text);
        Assert.True(left.Data.IsBeforeEol);

        // world\n
        Assert.Single(left.Children);
        TreeNode<TextSpanPayload> right = left.Children[0];
        Assert.Equal("world", right.Data!.Text);
        Assert.True(right.Data.IsBeforeEol);

        Assert.Empty(right.Children);
    }

    [Fact]
    public void Apply_MultipleNodesWithNewlines_SplitsNodes()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpanPayload> root = CreateNode("Hello\n");
        TreeNode<TextSpanPayload> child = CreateNode("world\nagain");
        root.AddChild(child);

        TreeNode<TextSpanPayload> result = filter.Apply(root, new Item());

        // Hello\n
        Assert.Single(result.Children);
        TreeNode<TextSpanPayload> left = result.Children[0];
        Assert.Equal("Hello", left.Data!.Text);
        Assert.True(left.Data.IsBeforeEol);

        // world\n
        Assert.Single(left.Children);
        TreeNode<TextSpanPayload> right = left.Children[0];
        Assert.Equal("world", right.Data!.Text);
        Assert.True(right.Data.IsBeforeEol);

        // again
        Assert.Single(right.Children);
        TreeNode<TextSpanPayload> rightChild = right.Children[0];
        Assert.Equal("again", rightChild.Data!.Text);
        Assert.False(rightChild.Data.IsBeforeEol);

        Assert.Empty(rightChild.Children);
    }

    [Fact]
    public void Apply_TextStartingWithNewline_SplitsNodes()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpanPayload> root = CreateNode("\nHello\nworld");

        TreeNode<TextSpanPayload> result = filter.Apply(root, new Item());

        // \n
        Assert.Single(result.Children);
        TreeNode<TextSpanPayload> first = result.Children[0];
        Assert.Equal("", first.Data!.Text);
        Assert.True(first.Data.IsBeforeEol);

        // Hello\n
        Assert.Single(first.Children);
        TreeNode<TextSpanPayload> second = first.Children[0];
        Assert.Equal("Hello", second.Data!.Text);
        Assert.True(second.Data.IsBeforeEol);

        // world
        Assert.Single(second.Children);
        TreeNode<TextSpanPayload> third = second.Children[0];
        Assert.Equal("world", third.Data!.Text);
        Assert.False(third.Data.IsBeforeEol);

        Assert.Empty(third.Children);
    }

    [Fact]
    public void Apply_EmptyText_ReturnsNodeUnchanged()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpanPayload> root = CreateNode("");

        TreeNode<TextSpanPayload> result = filter.Apply(root, new Item());

        Assert.Single(result.Children);
        Assert.Equal("", result.Children[0].Data!.Text);
        Assert.Empty(result.Children[0].Children);
    }

    [Fact]
    public void Apply_ConsecutiveNewlines_SplitsNodes()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpanPayload> root = CreateNode("Hello\n\nworld");

        TreeNode<TextSpanPayload> result = filter.Apply(root, new Item());

        // Hello\n
        Assert.Single(result.Children);
        TreeNode<TextSpanPayload> first = result.Children[0];
        Assert.Equal("Hello", first.Data!.Text);
        Assert.True(first.Data.IsBeforeEol);

        // \n
        Assert.Single(first.Children);
        TreeNode<TextSpanPayload> second = first.Children[0];
        Assert.Equal("", second.Data!.Text);
        Assert.True(second.Data.IsBeforeEol);

        // world
        Assert.Single(second.Children);
        TreeNode<TextSpanPayload> third = second.Children[0];
        Assert.Equal("world", third.Data!.Text);
        Assert.False(third.Data.IsBeforeEol);

        Assert.Empty(third.Children);
    }

    [Fact]
    public void Apply_NullTextInPayload_HandlesGracefully()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpanPayload> node = new(new TextSpanPayload(
            new FragmentTextRange(0, 0)) { Text = null });

        TreeNode<TextSpanPayload> result = filter.Apply(node, new Item());

        Assert.Single(result.Children);
        Assert.Null(result.Children[0].Data!.Text);
        Assert.Empty(result.Children[0].Children);
    }

    [Fact]
    public void Apply_NewlineOnlyNode_Removed()
    {
        // que bixit|annos XX
        BlockLinearTextTreeFilter filter = new();
        // que
        TreeNode<TextSpanPayload> root = CreateNode("que");
        TreeNode<TextSpanPayload> current = root;
        // space
        TreeNode<TextSpanPayload> node = CreateNode(" ");
        current.AddChild(node);
        // bixit
        node = CreateNode("bixit");
        current.AddChild(node);
        current = node;
        // LF
        node = CreateNode("\n");
        current.AddChild(node);
        current = node;
        // annos
        node = CreateNode("annos");
        current.AddChild(node);
        current = node;
        // space + XX
        node = CreateNode(" XX");
        current.AddChild(node);

        TreeNode<TextSpanPayload> result = filter.Apply(root, new Item());

        // root
        Assert.Single(result.Children);
        // que
        TreeNode<TextSpanPayload> que = result.Children[0];
        Assert.Equal("que", que.Data!.Text);
        // space
        Assert.Single(que.Children);
        TreeNode<TextSpanPayload> space = que.Children[0];
        Assert.Equal(" ", space.Data!.Text);
        // bixit
        Assert.Single(space.Children);
        TreeNode<TextSpanPayload> bixit = space.Children[0];
        Assert.Equal("bixit", bixit.Data!.Text);
        Assert.True(bixit.Data.IsBeforeEol);
        // annos
        Assert.Single(bixit.Children);
        TreeNode<TextSpanPayload> annos = bixit.Children[0];
        Assert.Equal("annos", annos.Data!.Text);
        // space + XX
        Assert.Single(annos.Children);
        TreeNode<TextSpanPayload> xx = annos.Children[0];
        Assert.Equal(" XX", xx.Data!.Text);
        Assert.False(xx.HasChildren);
    }
}