using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) { return (null, null); }
        
        int compareRootToKey = Comparer.Compare(root.Key, key);
        if (compareRootToKey <= 0)
        {
            var (leftOfRight, rightPart) = Split(root.Right, key);
            root.Right = leftOfRight;
            if (leftOfRight != null) { leftOfRight.Parent = root; }
            root.Parent = null;
            if (rightPart != null) { rightPart.Parent = null; }
            return (root, rightPart);
        }
        else
        {
            var (leftPart, rightOfLeft) = Split(root.Left, key);
            root.Left = rightOfLeft;
            if (rightOfLeft != null) { rightOfLeft.Parent = root; }
            root.Parent = null;
            if (leftPart != null) { leftPart.Parent = null; }
            return (leftPart, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null) { return right; }
        if (right == null) { return left; }
        
        if (left.Priority >= right.Priority)
        {
            var mergedRight = Merge(left.Right, right);
            left.Right = mergedRight;
            if (mergedRight != null) { mergedRight.Parent = left; }
            left.Parent = null;
            return left;
        }
        else
        {
            var mergedLeft = Merge(left, right.Left);
            right.Left = mergedLeft;
            if (mergedLeft != null) { mergedLeft.Parent = right; }
            right.Parent = null;
            return right;
        }
    }
    

    public override void Add(TKey key, TValue value)
    {
        var existingNode = FindNode(key);
        if (existingNode != null)
        {
            existingNode.Value = value;
            return;
        }

        var newNode = CreateNode(key, value);
        var (leftPart, rightPart) = Split(Root, key);
        Root = Merge(Merge(leftPart, newNode), rightPart);
        if (Root != null) { Root.Parent = null; }
        Count++;
        OnNodeAdded(newNode);
    }

    public override bool Remove(TKey key)
    {
        bool removed = false;
        Root = Erase(Root, key, ref removed);
        if (Root != null) { Root.Parent = null; }
        if (!removed) { return false; }
        Count--;
        return true;
    }

    private TreapNode<TKey, TValue>? Erase(TreapNode<TKey, TValue>? currentRoot, TKey key, ref bool removed)
    {
        if (currentRoot == null) { return null; }

        int compareKeyToRoot = Comparer.Compare(key, currentRoot.Key);
        if (compareKeyToRoot == 0)
        {
            removed = true;
            var mergedSubtree = Merge(currentRoot.Left, currentRoot.Right);
            if (mergedSubtree != null) { mergedSubtree.Parent = currentRoot.Parent; }
            OnNodeRemoved(currentRoot.Parent, mergedSubtree);
            return mergedSubtree;
        }

        if (compareKeyToRoot < 0)
        {
            currentRoot.Left = Erase(currentRoot.Left, key, ref removed);
            if (currentRoot.Left != null) { currentRoot.Left.Parent = currentRoot; }
        }
        else
        {
            currentRoot.Right = Erase(currentRoot.Right, key, ref removed);
            if (currentRoot.Right != null) { currentRoot.Right.Parent = currentRoot; }
        }

        return currentRoot;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode)
    {
    }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child)
    {
    }
    
}