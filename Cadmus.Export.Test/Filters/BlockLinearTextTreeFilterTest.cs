using Cadmus.Core;
using Cadmus.Export.Filters;
using Fusi.Tools.Data;
using Xunit;

namespace Cadmus.Export.Test.Filters;

public class BlockLinearTextTreeFilterTests
{
    private static TreeNode<TextSpanPayload> CreateNode(string text)
    {
        return new TreeNode<TextSpanPayload>(new TextSpanPayload(
            new FragmentTextRange(0, text.Length)) { Text = text });
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
        Assert.Equal("Hello\n", left.Data!.Text);

        // world
        Assert.Single(left.Children);
        TreeNode<TextSpanPayload> right = left.Children[0];
        Assert.Equal("world", right.Data!.Text);

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
        Assert.Equal("Hello\n", left.Data!.Text);

        // world\n
        Assert.Single(left.Children);
        TreeNode<TextSpanPayload> middle = left.Children[0];
        Assert.Equal("world\n", middle.Data!.Text);

        // again
        Assert.Single(middle.Children);
        TreeNode<TextSpanPayload> right = middle.Children[0];
        Assert.Equal("again", right.Data!.Text);

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
        Assert.Equal("Hello\n", left.Data!.Text);

        // world\n
        Assert.Single(left.Children);
        TreeNode<TextSpanPayload> right = left.Children[0];
        Assert.Equal("world\n", right.Data!.Text);

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
        Assert.Equal("Hello\n", left.Data!.Text);

        // world\n
        Assert.Single(left.Children);
        TreeNode<TextSpanPayload> right = left.Children[0];
        Assert.Equal("world\n", right.Data!.Text);

        // again
        Assert.Single(right.Children);
        TreeNode<TextSpanPayload> rightChild = right.Children[0];
        Assert.Equal("again", rightChild.Data!.Text);

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
        Assert.Equal("\n", first.Data!.Text);

        // Hello\n
        Assert.Single(first.Children);
        TreeNode<TextSpanPayload> second = first.Children[0];
        Assert.Equal("Hello\n", second.Data!.Text);

        // world
        Assert.Single(second.Children);
        TreeNode<TextSpanPayload> third = second.Children[0];
        Assert.Equal("world", third.Data!.Text);

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
        Assert.Equal("Hello\n", first.Data!.Text);

        // \n
        Assert.Single(first.Children);
        TreeNode<TextSpanPayload> second = first.Children[0];
        Assert.Equal("\n", second.Data!.Text);

        // world
        Assert.Single(second.Children);
        TreeNode<TextSpanPayload> third = second.Children[0];
        Assert.Equal("world", third.Data!.Text);

        Assert.Empty(third.Children);
    }

    [Fact]
    public void Apply_NullTextInPayload_HandlesGracefully()
    {
        BlockLinearTextTreeFilter filter = new();
        var node = new TreeNode<TextSpanPayload>(new TextSpanPayload(
            new FragmentTextRange(0, 0)) { Text = null });

        TreeNode<TextSpanPayload> result = filter.Apply(node, new Item());

        Assert.Single(result.Children);
        Assert.Null(result.Children[0].Data!.Text);
        Assert.Empty(result.Children[0].Children);
    }
}