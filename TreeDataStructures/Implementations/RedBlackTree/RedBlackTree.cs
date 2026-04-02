using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new RbNode<TKey, TValue>(key, value) { Color = RbColor.Red };
    }
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        FixInsert(newNode);
    }
    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        
    }

    public override void Add(TKey key, TValue value)
    {
        if (Root == null)
        {
            Root = CreateNode(key, value);
            Root.Color = RbColor.Black;
            Count = 1;
            return;
        }

        RbNode<TKey, TValue>? current = Root;
        RbNode<TKey, TValue>? parent = null;

        while (current != null)
        {
            parent = current;
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                current.Value = value;
                return;
            }
            current = cmp < 0 ? current.Left : current.Right;
        }

        var node = CreateNode(key, value);
        node.Parent = parent;
        if (Comparer.Compare(key, parent!.Key) < 0) { parent.Left = node; }
        else { parent.Right = node; }

        Count++;
        FixInsert(node);
    }

    public override bool Remove(TKey key)
    {
        var nodeToDelete = FindNode(key);
        if (nodeToDelete == null) { return false; }

        DeleteNode(nodeToDelete);
        Count--;
        return true;
    }

    private static RbColor ColorOf(RbNode<TKey, TValue>? node) => node?.Color ?? RbColor.Black;
    private static void SetColor(RbNode<TKey, TValue>? node, RbColor color) { if (node != null) { node.Color = color; } }
    private static RbNode<TKey, TValue>? ParentOf(RbNode<TKey, TValue>? node) => node?.Parent;

    private void FixInsert(RbNode<TKey, TValue> insertedNode)
    {
        while (insertedNode.Parent != null && insertedNode.Parent.Color == RbColor.Red)
        {
            var parentNode = insertedNode.Parent;
            var grandParentNode = parentNode.Parent;
            if (grandParentNode == null) { break; }

            if (parentNode.IsLeftChild)
            {
                var uncleNode = grandParentNode.Right;
                if (ColorOf(uncleNode) == RbColor.Red)
                {
                    SetColor(parentNode, RbColor.Black);
                    SetColor(uncleNode, RbColor.Black);
                    SetColor(grandParentNode, RbColor.Red);
                    insertedNode = grandParentNode;
                }
                else
                {
                    if (insertedNode.IsRightChild)
                    {
                        insertedNode = parentNode;
                        RotateLeft(insertedNode);
                        parentNode = insertedNode.Parent!;
                        grandParentNode = parentNode.Parent!;
                    }
                    SetColor(parentNode, RbColor.Black);
                    SetColor(grandParentNode, RbColor.Red);
                    RotateRight(grandParentNode);
                }
            }
            else
            {
                var uncleNode = grandParentNode.Left;
                if (ColorOf(uncleNode) == RbColor.Red)
                {
                    SetColor(parentNode, RbColor.Black);
                    SetColor(uncleNode, RbColor.Black);
                    SetColor(grandParentNode, RbColor.Red);
                    insertedNode = grandParentNode;
                }
                else
                {
                    if (insertedNode.IsLeftChild)
                    {
                        insertedNode = parentNode;
                        RotateRight(insertedNode);
                        parentNode = insertedNode.Parent!;
                        grandParentNode = parentNode.Parent!;
                    }
                    SetColor(parentNode, RbColor.Black);
                    SetColor(grandParentNode, RbColor.Red);
                    RotateLeft(grandParentNode);
                }
            }
        }

        if (Root != null) { Root.Color = RbColor.Black; }
    }

    private RbNode<TKey, TValue> Minimum(RbNode<TKey, TValue> node)
    {
        while (node.Left != null) { node = node.Left; }
        return node;
    }

    private void DeleteNode(RbNode<TKey, TValue> nodeToDelete)
    {
        var replacementCandidate = nodeToDelete;
        var replacementOriginalColor = replacementCandidate.Color;

        RbNode<TKey, TValue>? replacementChild;
        RbNode<TKey, TValue>? replacementChildParent;

        if (nodeToDelete.Left == null)
        {
            replacementChild = nodeToDelete.Right;
            replacementChildParent = nodeToDelete.Parent;
            Transplant(nodeToDelete, nodeToDelete.Right);
            OnNodeRemoved(replacementChildParent, replacementChild);
        }
        else if (nodeToDelete.Right == null)
        {
            replacementChild = nodeToDelete.Left;
            replacementChildParent = nodeToDelete.Parent;
            Transplant(nodeToDelete, nodeToDelete.Left);
            OnNodeRemoved(replacementChildParent, replacementChild);
        }
        else
        {
            replacementCandidate = Minimum(nodeToDelete.Right);
            replacementOriginalColor = replacementCandidate.Color;
            replacementChild = replacementCandidate.Right;

            if (replacementCandidate.Parent == nodeToDelete)
            {
                replacementChildParent = replacementCandidate;
                if (replacementChild != null) { replacementChild.Parent = replacementCandidate; }
            }
            else
            {
                replacementChildParent = replacementCandidate.Parent;
                Transplant(replacementCandidate, replacementCandidate.Right);
                replacementCandidate.Right = nodeToDelete.Right;
                replacementCandidate.Right!.Parent = replacementCandidate;
            }

            Transplant(nodeToDelete, replacementCandidate);
            replacementCandidate.Left = nodeToDelete.Left;
            replacementCandidate.Left!.Parent = replacementCandidate;
            replacementCandidate.Color = nodeToDelete.Color;
            OnNodeRemoved(replacementCandidate.Parent, replacementCandidate);
        }

        if (replacementOriginalColor == RbColor.Black)
        {
            FixDelete(replacementChild, replacementChildParent);
        }

        if (Root != null) { Root.Color = RbColor.Black; }
    }

    private void FixDelete(RbNode<TKey, TValue>? currentNode, RbNode<TKey, TValue>? parentNode)
    {
        while (currentNode != Root && ColorOf(currentNode) == RbColor.Black)
        {
            if (parentNode == null) { break; }

            if (ReferenceEquals(currentNode, parentNode.Left))
            {
                var siblingNode = parentNode.Right;
                if (ColorOf(siblingNode) == RbColor.Red)
                {
                    SetColor(siblingNode, RbColor.Black);
                    SetColor(parentNode, RbColor.Red);
                    RotateLeft(parentNode);
                    siblingNode = parentNode.Right;
                }

                if (ColorOf(siblingNode?.Left) == RbColor.Black && ColorOf(siblingNode?.Right) == RbColor.Black)
                {
                    SetColor(siblingNode, RbColor.Red);
                    currentNode = parentNode;
                    parentNode = ParentOf(currentNode);
                }
                else
                {
                    if (ColorOf(siblingNode?.Right) == RbColor.Black)
                    {
                        SetColor(siblingNode?.Left, RbColor.Black);
                        SetColor(siblingNode, RbColor.Red);
                        if (siblingNode != null) { RotateRight(siblingNode); }
                        siblingNode = parentNode.Right;
                    }

                    SetColor(siblingNode, ColorOf(parentNode));
                    SetColor(parentNode, RbColor.Black);
                    SetColor(siblingNode?.Right, RbColor.Black);
                    RotateLeft(parentNode);
                    currentNode = Root;
                    break;
                }
            }
            else
            {
                var siblingNode = parentNode.Left;
                if (ColorOf(siblingNode) == RbColor.Red)
                {
                    SetColor(siblingNode, RbColor.Black);
                    SetColor(parentNode, RbColor.Red);
                    RotateRight(parentNode);
                    siblingNode = parentNode.Left;
                }

                if (ColorOf(siblingNode?.Right) == RbColor.Black && ColorOf(siblingNode?.Left) == RbColor.Black)
                {
                    SetColor(siblingNode, RbColor.Red);
                    currentNode = parentNode;
                    parentNode = ParentOf(currentNode);
                }
                else
                {
                    if (ColorOf(siblingNode?.Left) == RbColor.Black)
                    {
                        SetColor(siblingNode?.Right, RbColor.Black);
                        SetColor(siblingNode, RbColor.Red);
                        if (siblingNode != null) { RotateLeft(siblingNode); }
                        siblingNode = parentNode.Left;
                    }

                    SetColor(siblingNode, ColorOf(parentNode));
                    SetColor(parentNode, RbColor.Black);
                    SetColor(siblingNode?.Left, RbColor.Black);
                    RotateRight(parentNode);
                    currentNode = Root;
                    break;
                }
            }
        }

        SetColor(currentNode, RbColor.Black);
    }
}