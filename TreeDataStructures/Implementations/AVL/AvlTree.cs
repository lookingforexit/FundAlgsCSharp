using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        RebalanceUpwards(newNode.Parent);
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        RebalanceUpwards(parent ?? child);
    }

    private void RebalanceUpwards(AvlNode<TKey, TValue>? node)
    {
        while (node != null)
        {
            UpdateHeight(node);
            int balanceFactor = BalanceFactor(node);

            if (balanceFactor > 1)
            {
                if (node.Left != null && BalanceFactor(node.Left) < 0)
                {
                    RotateLeftAvl(node.Left);
                }
                RotateRightAvl(node);
            }
            else if (balanceFactor < -1)
            {
                if (node.Right != null && BalanceFactor(node.Right) > 0)
                {
                    RotateRightAvl(node.Right);
                }
                RotateLeftAvl(node);
            }

            node = node.Parent;
        }
    }

    private static int HeightOf(AvlNode<TKey, TValue>? node) => node?.Height ?? 0;

    private static void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        int leftHeight = HeightOf(node.Left);
        int rightHeight = HeightOf(node.Right);
        node.Height = (leftHeight > rightHeight ? leftHeight : rightHeight) + 1;
    }

    private static int BalanceFactor(AvlNode<TKey, TValue> node) => HeightOf(node.Left) - HeightOf(node.Right);

    private void RotateLeftAvl(AvlNode<TKey, TValue> pivotNode)
    {
        var newParentNode = pivotNode.Right;
        RotateLeft(pivotNode);
        UpdateHeight(pivotNode);
        if (newParentNode != null) { UpdateHeight(newParentNode); }
    }

    private void RotateRightAvl(AvlNode<TKey, TValue> pivotNode)
    {
        var newParentNode = pivotNode.Left;
        RotateRight(pivotNode);
        UpdateHeight(pivotNode);
        if (newParentNode != null) { UpdateHeight(newParentNode); }
    }
    
}