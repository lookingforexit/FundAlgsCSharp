using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => throw new NotImplementedException();
    public ICollection<TValue> Values => throw new NotImplementedException();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        if (Root == null)
        {
            Root = CreateNode(key, value);
            ++Count;
            OnNodeAdded(Root);
            return;
        }
        
        TNode? current = Root;
        TNode? parent = null;

        while (current != null)
        {
            parent = current;
            current = Comparer.Compare(key, current.Key) < 0 ?  current.Left : current.Right;
        }
        
        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;

        if (Comparer.Compare(key, parent!.Key) < 0)
        {
            parent.Left = newNode;
        }
        else
        {
            parent.Right = newNode;
        }

        Count++;
        OnNodeAdded(newNode);
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null)
        {
            Transplant(node, node.Right);
            OnNodeRemoved(node.Parent, node.Right);
        } 
        else if (node.Right == null)
        {
            Transplant(node, node.Left);
            OnNodeRemoved(node.Parent, node.Left);
        }
        else
        {
            TNode replacement = FindMin(node.Right);
            if (replacement.Parent != node)
            {
                Transplant(node, replacement);
                replacement.Right = node.Right;
                replacement.Right.Parent = replacement;
            }
            
            Transplant(node, replacement);
            replacement.Left = node.Left;
            replacement.Left.Parent = replacement;
            
            OnNodeRemoved(node.Parent, replacement);
        }
    }

    private TNode FindMin(TNode node)
    {
        while (node.Left != null)
        {
            node = node.Left;
        }

        return node;
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x.Right == null)
        {
            return;
        }
        
        TNode rightTree = x.Right;
        TNode? centerTree = rightTree.Left;

        x.Right = centerTree;
        if (centerTree != null)
        {
            centerTree.Parent = x;
        }

        rightTree.Parent = x.Parent;

        if (x.Parent == null)
        {
            Root = rightTree;
        } else if (x.IsLeftChild)
        {
            x.Parent.Left = rightTree;
        }
        else
        {
            x.Parent.Right = rightTree;
        }

        rightTree.Left = x;
        x.Parent = rightTree;
        
        //update height for both trees
    }

    protected void RotateRight(TNode y)
    {
        if (y.Left == null)
        {
            return;
        }
        
        TNode leftTree = y.Left;
        TNode? centerTree = leftTree.Right;

        y.Left = centerTree;
        if (centerTree != null)
        {
            centerTree.Parent = y;
        }
        
        leftTree.Parent = y.Parent;
        if (y.Parent == null)
        {
            Root = leftTree;
        } else if (y.IsLeftChild)
        {
            y.Parent.Left = leftTree;
        }
        else
        {
            y.Parent.Right = leftTree;
        }

        y.Parent = leftTree;
        leftTree.Right = y;

        //update height for both trees
    }
    
    protected void RotateBigLeft(TNode x)
    {
        if (x.Right == null)
        {
            return;
        }
        
        RotateRight(x.Right);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        if (y.Left == null)
        {
            return;
        }
        
        RotateLeft(y.Left);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        RotateLeft(x);
        RotateLeft(x);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        RotateRight(y);
        RotateRight(y);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder() => InOrderTraversal(Root);
    
    private IEnumerable<TreeEntry<TKey, TValue>>  InOrderTraversal(TNode? node)
    {
        throw new NotImplementedException();
    }
    
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder() => PreOrderTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> PreOrderTraversal(TNode? node)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => PostOrderTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> PostOrderTraversal(TNode? node)
    {
        throw new NotImplementedException();
    }
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => InOrderReverseTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> InOrderReverseTraversal(TNode? node)
    {
        throw new NotImplementedException();
    }
    
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => PreOrderReverseTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverseTraversal(TNode? node)
    {
        throw new NotImplementedException();
    }
    
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => PostOrderReverseTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverseTraversal(TNode? node)
    {
        throw new NotImplementedException();
    }
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        // probably add something here
        private readonly TraversalStrategy _strategy; // or make it template parameter?
        
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current => throw new NotImplementedException();
        object IEnumerator.Current => Current;
        
        
        public bool MoveNext()
        {
            if (_strategy == TraversalStrategy.InOrder)
            {
                throw new NotImplementedException();
            }
            throw new NotImplementedException("Strategy not implemented");
        }
        
        public void Reset()
        {
            throw new NotImplementedException();
        }

        
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        throw new NotImplementedException();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}