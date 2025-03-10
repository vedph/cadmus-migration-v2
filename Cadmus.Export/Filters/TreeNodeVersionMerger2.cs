using System;
using System.Collections.Generic;
using System.Linq;

// TEMP COPY FROM Fusi.Tools

namespace Fusi.Tools.Data;

/// <summary>
/// A class used to transform a tree of <see cref="TreeNode{T}"/> by following
/// traversal paths defined by a list of tags. This class assumes that the
/// received <see cref="ITreeNodePayloadTagger{T}"/> instance can be used to
/// keep a list of tags in the payload of each node, and to compare some subset
/// of payload data to use as the value for branching.
/// </summary>
/// <typeparam name="T">The node payload data type.</typeparam>
/// <remarks>
/// Each tag defines a specific sequence of payload values, which can be derived
/// by collecting the value from the payload of each node in a specific tree
/// traversal. For instance, if payload values are characters, and we have a
/// tree like:
///     A -> B -> C, D
/// then a path for tag "v1" could be "ABC", and for tag "v2" "ABD". For each
/// tag, the builder receives a branch of nodes, and it must merge them into
/// a tree where all nodes are reused for each version up to the point where
/// there is a value mismatch. When a mismatch is found, a new branch is created.
/// When operating in binary mode, the builder will allow at most 2 children per
/// node; in that case, when more than 2 children are needed, it inserts a
/// blank fork node at each branching point, having as first child the original
/// node, and as second child the new one.
/// </remarks>
/// <param name="tagger">The tagger.</param>
/// <param name="idGenerator">The optional tree node ID generator. When not
/// specified, a default numeric ID generator will be used.</param>
/// <exception cref="ArgumentNullException">tagger</exception>
public class TreeNodeVersionMerger2<T>(ITreeNodePayloadTagger<T> tagger,
    ITreeNodeIdGenerator<T>? idGenerator = null)
{
    private readonly ITreeNodePayloadTagger<T> _tagger = tagger
        ?? throw new ArgumentNullException(nameof(tagger));
    private readonly ITreeNodeIdGenerator<T> _idGenerator = idGenerator
        ?? new NumericTreeNodeIdGenerator<T>();

    /// <summary>
    /// Creates a node branch for the specified payloads.
    /// </summary>
    /// <param name="payloads">The payloads to add to the new branch.</param>
    /// <param name="nodes">The original nodes corresponding to payloads.</param>
    /// <param name="startIndex">The start node index in payloads.</param>
    /// <param name="tag">The version tag.</param>
    /// <returns>Branch node or null.</returns>
    private TreeNode<T>? CreateNodeBranch(List<T> payloads,
        List<TreeNode<T>> nodes, int startIndex, string tag)
    {
        if (startIndex >= payloads.Count) return null;

        // create first node
        T firstNode = payloads[startIndex];
        _tagger.AddTag(firstNode, tag);

        TreeNode<T> branch = new(firstNode)
        {
            Id = nodes[startIndex].Id,
            Label = nodes[startIndex].Label
        };

        // add remaining nodes
        TreeNode<T> current = branch;
        for (int i = startIndex + 1; i < payloads.Count; i++)
        {
            T node = payloads[i];
            _tagger.AddTag(node, tag);

            TreeNode<T> child = new(node)
            {
                Id = nodes[i].Id,
                Label = nodes[i].Label
            };
            current.AddChild(child);
            current = child;
        }

        return branch;
    }

    private void AddBranchToBinaryTree(TreeNode<T> parent,
        List<T> payloads, List<TreeNode<T>> nodes, int startIndex, string tag)
    {
        if (startIndex >= payloads.Count) return;

        // create new branch
        TreeNode<T>? newBranch =
            CreateNodeBranch(payloads, nodes, startIndex, tag);
        if (newBranch == null) return;

        // if parent has no children, just add the branch
        if (!parent.HasChildren)
        {
            parent.AddChild(newBranch);
            return;
        }

        // if parent already has a blank fork node child, use it
        if (parent.Children.Count == 1 && parent.Children[0].Data is null)
        {
            TreeNode<T> fork = parent.Children[0];
            if (fork.Children.Count < 2)
            {
                fork.AddChild(newBranch);
            }
            else
            {
                // create new fork node
                TreeNode<T> newFork = new();
                TreeNode<T> secondChild = fork.Children[1];
                fork.Children[1] = newFork;
                newFork.Parent = fork;
                newFork.AddChild(secondChild);
                newFork.AddChild(newBranch);
            }
            return;
        }

        // create new blank fork node
        TreeNode<T> blankFork = new();

        // if parent has one child, move it under fork
        if (parent.Children.Count == 1)
        {
            TreeNode<T> existingChild = parent.Children[0];
            parent.Children.Clear();
            parent.AddChild(blankFork);
            blankFork.AddChild(existingChild);
            blankFork.AddChild(newBranch);
        }
        // else parent has multiple children
        else
        {
            List<TreeNode<T>> existingChildren = [.. parent.Children];
            parent.Children.Clear();
            parent.AddChild(blankFork);

            // first child gets original branches
            TreeNode<T> subFork = new();
            blankFork.AddChild(subFork);
            foreach (TreeNode<T>? child in existingChildren)
                subFork.AddChild(child);

            // second child gets new branch
            blankFork.AddChild(newBranch);
        }
    }

    private (TreeNode<T> node, int matchLength) FindCommonPrefix(
        TreeNode<T> root, string tag, List<T> payloads, bool binary)
    {
        // no payloads or no children: return root
        if (payloads.Count == 0 || !root.HasChildren) return (root, 0);

        TreeNode<T> current = root;
        int matchLength = 1;  // start with 1 as we're checking children

        while (current.HasChildren && matchLength < payloads.Count)
        {
            bool foundMatch = false;
            foreach (TreeNode<T> child in current.Children)
            {
                if (child.Data is null)
                {
                    // for blank fork nodes, use the SAME payloads index
                    // but with adjusted skipping in the recursive call
                    (TreeNode<T> deepNode, int deepMatchLength) =
                        FindCommonPrefix(child, tag,
                            [.. payloads.Skip(matchLength - 1)],
                        binary);

                    if (deepMatchLength > 0)
                    {
                        current = deepNode;
                        // only add deepMatchLength - 1 because we've already
                        // accounted for one match
                        matchLength += deepMatchLength - 1;
                        foundMatch = true;
                        break;
                    }
                }
                else if (!_tagger.HasPayloadValue(payloads[matchLength]) ||
                         _tagger.MatchPayloadValues(tag, child.Data,
                            payloads[matchLength]))
                {
                    current = child;
                    matchLength++;
                    foundMatch = true;
                    break;
                }
            }
            if (!foundMatch) break;
        }

        return (current, matchLength);
    }

    private TreeNode<T>? FindChildMatchingPayload(TreeNode<T> node, string tag,
        T payload)
    {
        if (node.HasChildren)
        {
            foreach (TreeNode<T> existingChild in node.Children)
            {
                if (existingChild.Data is not null &&
                    _tagger.MatchPayloadValues(tag, existingChild.Data, payload))
                {
                    // use existing child
                    _tagger.AddTag(existingChild.Data, tag);
                    return existingChild;
                }
            }
        }

        return null;
    }

    private void PropagateTagUp(TreeNode<T> node, string tag)
    {
        TreeNode<T>? current = node.Parent;

        while (current != null)
        {
            if (current.Data is not null) _tagger.AddTag(current.Data!, tag);
            current = current.Parent;
        }
    }

    private void SupplyIds(TreeNode<T> node)
    {
        node.Id ??= _idGenerator.GetNextId();

        if (node.HasChildren)
        {
            foreach (TreeNode<T> child in node.Children)
                SupplyIds(child);
        }
    }

    /// <summary>
    /// Merges into the specified root node the alternative version represented
    /// by the payloads of the linear branch starting with the
    /// <paramref name="version"/> node.
    /// </summary>
    /// <param name="root">The root.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="version">The version.</param>
    /// <param name="binary">True to use binary behavior, which allows max 2
    /// children per node. When more than 2 children are needed, we insert a
    /// blank fork node having as first child the original node and as second
    /// the new one.</param>
    /// <param name="init">if set to <c>true</c> to initialize the node ID seeder.
    /// This should be done only the first time you call this method in a
    /// merging session.</param>
    /// <exception cref="ArgumentNullException">root or tag or version</exception>
    public void Merge(TreeNode<T> root, string tag,
        TreeNode<T> version, bool binary, bool init)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(version);

        // seed ID generator when required
        if (binary && init) _idGenerator.Seed(root);

        // collect payloads from this branch with their corresponding nodes
        // (payloads are copies to be modified; nodes are used to get
        // IDs and labels)
        List<T> tagPayloads = [];
        List<TreeNode<T>> tagNodes = [];

        version.Traverse(node =>
        {
            if (node.Data is not null)
            {
                tagPayloads.Add(_tagger.ClonePayload(node.Data));
                tagNodes.Add(node);
            }
            return true;
        });

        // find where this version branches from existing tree
        (TreeNode<T> branchNode, int matchLength) =
            FindCommonPrefix(root, tag, tagPayloads, binary);

        // if all payloads match, just add the tag to the last node
        // and propagate it up
        if (matchLength == tagPayloads.Count)
        {
            branchNode.Data ??= _tagger.ClonePayload(
                tagPayloads[matchLength - 1]);
            _tagger.AddTag(branchNode.Data!, tag);
            PropagateTagUp(branchNode, tag);
            return;
        }

        // add version tag to matching nodes
        if (matchLength > 0)
        {
            branchNode.Data ??= _tagger.ClonePayload(
                tagPayloads[matchLength - 1]);
            _tagger.AddTag(branchNode.Data!, tag);

            // propagate tag to non-blank ascending nodes
            PropagateTagUp(branchNode, tag);
        }

        if (binary)
        {
            // add remaining nodes as a new binary branch
            AddBranchToBinaryTree(branchNode, tagPayloads, tagNodes,
                matchLength, tag);
        }
        else
        {
            // add remaining nodes to existing branch
            TreeNode<T> currentNode = branchNode;
            for (int i = matchLength; i < tagPayloads.Count; i++)
            {
                T payload = tagPayloads[i];
                _tagger.AddTag(payload, tag);

                // check if matching child already exists
                TreeNode<T>? existingChild = FindChildMatchingPayload(
                    currentNode, tag, payload);
                if (existingChild != null)
                {
                    currentNode = existingChild;
                }
                // if no matching child found, create new one
                else
                {
                    TreeNode<T> newChild = new(payload)
                    {
                        Id = tagNodes[i].Id,
                        Label = tagNodes[i].Label,
                    };
                    currentNode.AddChild(newChild);
                    currentNode = newChild;
                }
            }
        }

        // supply IDs to newly added nodes (which happens only for binary)
        if (binary) SupplyIds(root);
    }

    /// <summary>
    /// Builds a new tree by merging the specified tagged versions.
    /// </summary>
    /// <param name="tags">The version tags to follow.</param>
    /// <param name="pathGetFunc">The function used to get the path across all
    /// the nodes with the specified tag. The function receives a tag and must
    /// return the root node of the path.</param>
    /// <param name="binary">True to use binary behavior, which allows max 2
    /// children per node. When more than 2 children are needed, we insert a
    /// blank fork node having as first child the original node and as second
    /// the new one.</param>
    /// <returns>Root node of the new tree.</returns>
    /// <exception cref="ArgumentNullException">tags or nodeGetter</exception>
    public TreeNode<T> Merge(IList<string> tags,
        Func<string, TreeNode<T>> pathGetFunc,
        bool binary)
    {
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(pathGetFunc);
        TreeNode<T> root = new();

        int n = 0;
        foreach (string tag in tags)
        {
            TreeNode<T> original = pathGetFunc(tag);
            Merge(root, tag, original, binary, ++n == 1);
        }

        return root;
    }

    #region Dump
    /// <summary>
    /// Builds the code for a Graphviz tree diagram representing the received
    /// tree. You can use a visualizer like
    /// https://dreampuf.github.io/GraphvizOnline to see the result.
    /// </summary>
    /// <param name="root">The root node of the tree.</param>
    /// <param name="getNodeColorFunc">The optional node colorizer function.
    /// When not null, this will be called for getting the fill color of each
    /// node (e.g. <c>#ff9999</c>). Return null or empty for the default color.
    /// </param>
    /// <exception cref="ArgumentNullException">root</exception>
    public string DumpToGraph(TreeNode<T> root,
        Func<TreeNode<T>, string?>? getNodeColorFunc = null)
    {
        ArgumentNullException.ThrowIfNull(root);

        return root.DumpToGraph(getNodeColorFunc,
            node =>
            {
                if (node.Data is not null)
                {
                    List<string> tags = [.. _tagger.GetTags(node.Data).Order()];
                    if (tags.Count > 0) return string.Join(" ", tags);
                }
                return null;
            });
    }
    #endregion
}
