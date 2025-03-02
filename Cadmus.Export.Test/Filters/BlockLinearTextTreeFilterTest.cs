using Cadmus.Core;
using Cadmus.Export.Filters;
using Fusi.Tools.Data;
using Xunit;

namespace Cadmus.Export.Test.Filters;

public class BlockLinearTextTreeFilterTests
{
    private static TreeNode<TextSpan> CreateTextNode(string text)
    {
        return new TreeNode<TextSpan>(
            new TextSpan(
                // we do not care about range here, just use 0-length
                new AnnotatedTextRange(0, text.Length))
                {
                    Text = text
                });
    }

    [Fact]
    public void Apply_SingleNodeNoNewline_ReturnsSameNode()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpan> root = new();
        root.AddChild(CreateTextNode("Hello world"));

        TreeNode<TextSpan> result = filter.Apply(root, new Item());

        Assert.Single(result.Children);
        Assert.Equal("Hello world", result.Children[0].Data!.Text);

        Assert.Empty(result.Children[0].Children);
    }

    [Fact]
    public void Apply_SingleNodeWithNewline_SplitsNode()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpan> root = new();
        root.AddChild(CreateTextNode("Hello\nworld"));

        TreeNode<TextSpan> result = filter.Apply(root, new Item());

        // Hello\n
        Assert.Single(result.Children);
        TreeNode<TextSpan> left = result.Children[0];
        Assert.Equal("Hello", left.Data!.Text);
        Assert.True(left.Data.IsBeforeEol);

        // world
        Assert.Single(left.Children);
        TreeNode<TextSpan> right = left.Children[0];
        Assert.Equal("world", right.Data!.Text);
        Assert.False(right.Data.IsBeforeEol);

        Assert.Empty(right.Children);
    }

    [Fact]
    public void Apply_MultipleNewlines_SplitsNodes()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpan> root = new();
        root.AddChild(CreateTextNode("Hello\nworld\nagain"));

        TreeNode<TextSpan> result = filter.Apply(root, new Item());

        // Hello\n
        Assert.Single(result.Children);
        TreeNode<TextSpan> left = result.Children[0];
        Assert.Equal("Hello", left.Data!.Text);
        Assert.True(left.Data.IsBeforeEol);

        // world\n
        Assert.Single(left.Children);
        TreeNode<TextSpan> middle = left.Children[0];
        Assert.Equal("world", middle.Data!.Text);
        Assert.True(middle.Data.IsBeforeEol);

        // again
        Assert.Single(middle.Children);
        TreeNode<TextSpan> right = middle.Children[0];
        Assert.Equal("again", right.Data!.Text);
        Assert.False(right.Data.IsBeforeEol);

        Assert.Empty(right.Children);
    }

    [Fact]
    public void Apply_EndsWithNewline_SplitsNodes()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpan> root = new();
        root.AddChild(CreateTextNode("Hello\nworld\n"));

        TreeNode<TextSpan> result = filter.Apply(root, new Item());

        // Hello\n
        Assert.Single(result.Children);
        TreeNode<TextSpan> left = result.Children[0];
        Assert.Equal("Hello", left.Data!.Text);
        Assert.True(left.Data.IsBeforeEol);

        // world\n
        Assert.Single(left.Children);
        TreeNode<TextSpan> right = left.Children[0];
        Assert.Equal("world", right.Data!.Text);
        Assert.True(right.Data.IsBeforeEol);

        Assert.Empty(right.Children);
    }

    [Fact]
    public void Apply_MultipleNodesWithNewlines_SplitsNodes()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpan> root = new();
        root.AddChild(CreateTextNode("Hello\n"));
        TreeNode<TextSpan> child = CreateTextNode("world\nagain");
        root.AddChild(child);

        TreeNode<TextSpan> result = filter.Apply(root, new Item());

        // Hello\n
        Assert.Single(result.Children);
        TreeNode<TextSpan> left = result.Children[0];
        Assert.Equal("Hello", left.Data!.Text);
        Assert.True(left.Data.IsBeforeEol);

        // world\n
        Assert.Single(left.Children);
        TreeNode<TextSpan> right = left.Children[0];
        Assert.Equal("world", right.Data!.Text);
        Assert.True(right.Data.IsBeforeEol);

        // again
        Assert.Single(right.Children);
        TreeNode<TextSpan> rightChild = right.Children[0];
        Assert.Equal("again", rightChild.Data!.Text);
        Assert.False(rightChild.Data.IsBeforeEol);

        Assert.Empty(rightChild.Children);
    }

    [Fact]
    public void Apply_TextWithInitialNewline_EolAddedToParent()
    {
        BlockLinearTextTreeFilter filter = new();

        // create a tree where a node's text starts with a newline
        TreeNode<TextSpan> root = new();
        TreeNode<TextSpan> node1 = CreateTextNode("First node");
        root.AddChild(node1);

        // create a node whose text starts with a newline
        TreeNode<TextSpan> node2 = CreateTextNode("\nSecond node");
        node1.AddChild(node2);

        // apply the filter
        TreeNode<TextSpan> result = filter.Apply(root, new Item());

        // validate result structure:
        // - root
        //   - First node (with IsBeforeEol=true)
        //     - Second node (without the leading newline)

        // check root structure
        Assert.Single(result.Children);

        // check first node
        TreeNode<TextSpan> firstNode = result.Children[0];
        Assert.Equal("First node", firstNode.Data!.Text);
        Assert.True(firstNode.Data.IsBeforeEol,
            "First node should be marked with IsBeforeEol");
        Assert.Single(firstNode.Children);

        // check second node (should have the leading newline removed)
        TreeNode<TextSpan> secondNode = firstNode.Children[0];
        Assert.Equal("Second node", secondNode.Data!.Text);
        Assert.False(secondNode.Data.IsBeforeEol);

        // no more children
        Assert.Empty(secondNode.Children);
    }

    [Fact]
    public void Apply_TextWithInitialAndInternalNewline_SplitsNodes()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpan> root = new();
        root.AddChild(CreateTextNode("\nHello\nworld"));

        TreeNode<TextSpan> result = filter.Apply(root, new Item());

        // root
        Assert.NotNull(result.Data);
        Assert.True(result.Data.IsBeforeEol);

        // Hello\n
        Assert.Single(result.Children);
        TreeNode<TextSpan> hello = result.Children[0];
        Assert.Equal("Hello", hello.Data!.Text);
        Assert.True(hello.Data.IsBeforeEol);

        // world
        Assert.Single(hello.Children);
        TreeNode<TextSpan> world = hello.Children[0];
        Assert.Equal("world", world.Data!.Text);
        Assert.False(world.Data.IsBeforeEol);

        Assert.Empty(world.Children);
    }

    [Fact]
    public void Apply_EmptyText_ReturnsNodeUnchanged()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpan> root = new();
        root.AddChild(CreateTextNode(""));

        TreeNode<TextSpan> result = filter.Apply(root, new Item());

        Assert.Single(result.Children);
        Assert.Equal("", result.Children[0].Data!.Text);
        Assert.Empty(result.Children[0].Children);
    }

    [Fact]
    public void Apply_ConsecutiveNewlines_SplitsNodes()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpan> root = new();
        root.AddChild(CreateTextNode("Hello\n\nworld"));

        TreeNode<TextSpan> result = filter.Apply(root, new Item());

        // Hello\n
        Assert.Single(result.Children);
        TreeNode<TextSpan> first = result.Children[0];
        Assert.Equal("Hello", first.Data!.Text);
        Assert.True(first.Data.IsBeforeEol);

        // \n
        Assert.Single(first.Children);
        TreeNode<TextSpan> second = first.Children[0];
        Assert.Equal("", second.Data!.Text);
        Assert.True(second.Data.IsBeforeEol);

        // world
        Assert.Single(second.Children);
        TreeNode<TextSpan> third = second.Children[0];
        Assert.Equal("world", third.Data!.Text);
        Assert.False(third.Data.IsBeforeEol);

        Assert.Empty(third.Children);
    }

    [Fact]
    public void Apply_RootOnly_HandlesGracefully()
    {
        BlockLinearTextTreeFilter filter = new();
        TreeNode<TextSpan> root = new();

        TreeNode<TextSpan> result = filter.Apply(root, new Item());

        Assert.Empty(result.Children);
        Assert.Null(result.Data);
    }

    [Fact]
    public void Apply_NewlineOnlyNode_Removed()
    {
        // que bixit|annos XX
        BlockLinearTextTreeFilter filter = new();

        TreeNode<TextSpan> root = new();
        TreeNode<TextSpan> node;

        // que
        node = CreateTextNode("que");
        root.AddChild(node);
        TreeNode<TextSpan> current = node;

        // space
        node = CreateTextNode(" ");
        current.AddChild(node);
        current = node;

        // bixit
        node = CreateTextNode("bixit");
        current.AddChild(node);
        current = node;

        // LF
        node = CreateTextNode("\n");
        current.AddChild(node);
        current = node;

        // annos
        node = CreateTextNode("annos");
        current.AddChild(node);
        current = node;

        // space + XX
        node = CreateTextNode(" XX");
        current.AddChild(node);

        TreeNode<TextSpan> result = filter.Apply(root, new Item());

        // root
        Assert.Single(result.Children);
        // que
        TreeNode<TextSpan> que = result.Children[0];
        Assert.Equal("que", que.Data!.Text);
        // space
        Assert.Single(que.Children);
        TreeNode<TextSpan> space = que.Children[0];
        Assert.Equal(" ", space.Data!.Text);
        // bixit
        Assert.Single(space.Children);
        TreeNode<TextSpan> bixit = space.Children[0];
        Assert.Equal("bixit", bixit.Data!.Text);
        Assert.True(bixit.Data.IsBeforeEol);
        // annos
        Assert.Single(bixit.Children);
        TreeNode<TextSpan> annos = bixit.Children[0];
        Assert.Equal("annos", annos.Data!.Text);
        // space + XX
        Assert.Single(annos.Children);
        TreeNode<TextSpan> xx = annos.Children[0];
        Assert.Equal(" XX", xx.Data!.Text);
        Assert.False(xx.HasChildren);
    }
}