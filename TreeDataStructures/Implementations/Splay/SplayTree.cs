using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent != null)
        {
            Splay(parent);
        }
        else if (child != null)
        {
            Splay(child);
        }
    }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var foundNode = FindNode(key);
        if (foundNode == null)
        {
            value = default;
            return false;
        }

        Splay(foundNode);
        value = foundNode.Value;
        return true;
    }
    
    public override bool ContainsKey(TKey key)
    {
        var foundNode = FindNode(key);
        if (foundNode == null) { return false; }
        Splay(foundNode);
        return true;
    }

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent != null)
        {
            var parentNode = node.Parent;
            var grandParentNode = parentNode.Parent;

            if (grandParentNode == null)
            {
                if (node.IsLeftChild) { RotateRight(parentNode); }
                else { RotateLeft(parentNode); }
            }
            else if (node.IsLeftChild && parentNode.IsLeftChild)
            {
                RotateRight(grandParentNode);
                RotateRight(parentNode);
            }
            else if (node.IsRightChild && parentNode.IsRightChild)
            {
                RotateLeft(grandParentNode);
                RotateLeft(parentNode);
            }
            else if (node.IsRightChild && parentNode.IsLeftChild)
            {
                RotateLeft(parentNode);
                RotateRight(grandParentNode);
            }
            else
            {
                RotateRight(parentNode);
                RotateLeft(grandParentNode);
            }
        }
    }
}
