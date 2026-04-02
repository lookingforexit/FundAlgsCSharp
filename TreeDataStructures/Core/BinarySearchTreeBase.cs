using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default;

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys
    {
        get
        {
            var keys = new List<TKey>(Count);
            foreach (var e in InOrder())
            {
                keys.Add(e.Key);
            }
            return keys;
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            var values = new List<TValue>(Count);
            foreach (var e in InOrder())
            {
                values.Add(e.Value);
            }
            return values;
        }
    }
    
    
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
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                current.Value = value;
                return;
            }
            current = cmp < 0 ? current.Left : current.Right;
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
                Transplant(replacement, replacement.Right);
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
    
    protected virtual void OnNodeAdded(TNode newNode) { }
    
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

    protected void RotateLeft(TNode pivotNode)
    {
        if (pivotNode.Right == null)
        {
            return;
        }
        
        TNode rightSubtree = pivotNode.Right;
        TNode? centerSubtree = rightSubtree.Left;

        pivotNode.Right = centerSubtree;
        if (centerSubtree != null)
        {
            centerSubtree.Parent = pivotNode;
        }

        rightSubtree.Parent = pivotNode.Parent;

        if (pivotNode.Parent == null)
        {
            Root = rightSubtree;
        } else if (pivotNode.IsLeftChild)
        {
            pivotNode.Parent.Left = rightSubtree;
        }
        else
        {
            pivotNode.Parent.Right = rightSubtree;
        }

        rightSubtree.Left = pivotNode;
        pivotNode.Parent = rightSubtree;
    }

    protected void RotateRight(TNode pivotNode)
    {
        if (pivotNode.Left == null)
        {
            return;
        }
        
        TNode leftSubtree = pivotNode.Left;
        TNode? centerSubtree = leftSubtree.Right;

        pivotNode.Left = centerSubtree;
        if (centerSubtree != null)
        {
            centerSubtree.Parent = pivotNode;
        }
        
        leftSubtree.Parent = pivotNode.Parent;
        if (pivotNode.Parent == null)
        {
            Root = leftSubtree;
        } else if (pivotNode.IsLeftChild)
        {
            pivotNode.Parent.Left = leftSubtree;
        }
        else
        {
            pivotNode.Parent.Right = leftSubtree;
        }

        pivotNode.Parent = leftSubtree;
        leftSubtree.Right = pivotNode;
    }
    
    protected void RotateBigLeft(TNode pivotNode)
    {
        if (pivotNode.Right == null)
        {
            return;
        }
        
        RotateRight(pivotNode.Right);
        RotateLeft(pivotNode);
    }
    
    protected void RotateBigRight(TNode pivotNode)
    {
        if (pivotNode.Left == null)
        {
            return;
        }
        
        RotateLeft(pivotNode.Left);
        RotateRight(pivotNode);
    }
    
    protected void RotateDoubleLeft(TNode pivotNode)
    {
        RotateLeft(pivotNode);
        RotateLeft(pivotNode);
    }
    
    protected void RotateDoubleRight(TNode pivotNode)
    {
        RotateRight(pivotNode);
        RotateRight(pivotNode);
    }
    
    protected void Transplant(TNode replacedSubtreeRoot, TNode? replacementSubtreeRoot)
    {
        if (replacedSubtreeRoot.Parent == null)
        {
            Root = replacementSubtreeRoot;
        }
        else if (replacedSubtreeRoot.IsLeftChild)
        {
            replacedSubtreeRoot.Parent.Left = replacementSubtreeRoot;
        }
        else
        {
            replacedSubtreeRoot.Parent.Right = replacementSubtreeRoot;
        }
        replacementSubtreeRoot?.Parent = replacedSubtreeRoot.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    private readonly struct StackEntry(TNode node, int depth, bool visited = false)
    {
        public readonly TNode Node = node;
        public readonly int Depth = depth;
        public readonly bool Visited = visited;
    }

    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy;
        private readonly Stack<StackEntry> _stack;
        
        private TNode? _inOrderCursor;
        private int _inOrderDepth;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _stack = new Stack<StackEntry>();
            _inOrderCursor = null;
            _inOrderDepth = 0;
            Current = default;
            Reset();
        }
        
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current { get; private set; }

        object IEnumerator.Current => Current;
        
        public bool MoveNext()
        {
            if (_root == null)
                return false;

            return _strategy switch
            {
                TraversalStrategy.PreOrder => MoveNextPreOrder(),
                TraversalStrategy.InOrder => MoveNextInOrder(),
                TraversalStrategy.PostOrder => MoveNextPostOrder(),
                TraversalStrategy.PreOrderReverse => MoveNextPreOrderReverse(),
                TraversalStrategy.InOrderReverse => MoveNextInOrderReverse(),
                TraversalStrategy.PostOrderReverse => MoveNextPostOrderReverse(),
                _ => throw new NotSupportedException($"Traversal strategy '{_strategy}' unknown")
            };
        }
        
        private bool MoveNextPreOrder()
        {
            if (_stack.Count == 0)
                return false;
                
            var entry = _stack.Pop();
            Current = new TreeEntry<TKey, TValue>(entry.Node.Key, entry.Node.Value, entry.Depth);
            
            if (entry.Node.Right != null)
                _stack.Push(new StackEntry(entry.Node.Right, entry.Depth + 1));
            if (entry.Node.Left != null)
                _stack.Push(new StackEntry(entry.Node.Left, entry.Depth + 1));
                
            return true;
        }
        
        private bool MoveNextInOrder()
        {
            while (_inOrderCursor != null || _stack.Count > 0)
            {
                while (_inOrderCursor != null)
                {
                    _stack.Push(new StackEntry(_inOrderCursor, _inOrderDepth));
                    _inOrderCursor = _inOrderCursor.Left;
                    _inOrderDepth++;
                }
                
                if (_stack.Count == 0)
                    return false;
                    
                var entry = _stack.Pop();
                Current = new TreeEntry<TKey, TValue>(entry.Node.Key, entry.Node.Value, entry.Depth);
                
                _inOrderCursor = entry.Node.Right;
                _inOrderDepth = entry.Depth + 1;
                return true;
            }
            return false;
        }
        
        private bool MoveNextPostOrder()
        {
            while (_stack.Count > 0)
            {
                var entry = _stack.Pop();
                
                if (entry.Visited)
                {
                    Current = new TreeEntry<TKey, TValue>(entry.Node.Key, entry.Node.Value, entry.Depth);
                    return true;
                }
                
                _stack.Push(new StackEntry(entry.Node, entry.Depth, true));
                if (entry.Node.Right != null)
                    _stack.Push(new StackEntry(entry.Node.Right, entry.Depth + 1, false));
                if (entry.Node.Left != null)
                    _stack.Push(new StackEntry(entry.Node.Left, entry.Depth + 1, false));
            }
            return false;
        }
        
        private bool MoveNextPreOrderReverse()
        {
            if (_stack.Count == 0)
                return false;
                
            var entry = _stack.Pop();
            Current = new TreeEntry<TKey, TValue>(entry.Node.Key, entry.Node.Value, entry.Depth);
            
            if (entry.Node.Left != null)
                _stack.Push(new StackEntry(entry.Node.Left, entry.Depth + 1));
            if (entry.Node.Right != null)
                _stack.Push(new StackEntry(entry.Node.Right, entry.Depth + 1));
                
            return true;
        }
        
        private bool MoveNextInOrderReverse()
        {
            while (_inOrderCursor != null || _stack.Count > 0)
            {
                while (_inOrderCursor != null)
                {
                    _stack.Push(new StackEntry(_inOrderCursor, _inOrderDepth));
                    _inOrderCursor = _inOrderCursor.Right;
                    _inOrderDepth++;
                }
                
                if (_stack.Count == 0)
                    return false;
                    
                var entry = _stack.Pop();
                Current = new TreeEntry<TKey, TValue>(entry.Node.Key, entry.Node.Value, entry.Depth);
                
                _inOrderCursor = entry.Node.Left;
                _inOrderDepth = entry.Depth + 1;
                return true;
            }
            return false;
        }
        
        private bool MoveNextPostOrderReverse()
        {
            while (_stack.Count > 0)
            {
                var entry = _stack.Pop();
                
                if (entry.Visited)
                {
                    Current = new TreeEntry<TKey, TValue>(entry.Node.Key, entry.Node.Value, entry.Depth);
                    return true;
                }
                
                _stack.Push(new StackEntry(entry.Node, entry.Depth, true));
                if (entry.Node.Left != null)
                    _stack.Push(new StackEntry(entry.Node.Left, entry.Depth + 1, false));
                if (entry.Node.Right != null)
                    _stack.Push(new StackEntry(entry.Node.Right, entry.Depth + 1, false));
            }
            return false;
        }
        
        public void Reset()
        {
            _stack.Clear();
            _inOrderCursor = null;
            _inOrderDepth = 0;
            Current = default;
            
            if (_root == null)
                return;
                
            switch (_strategy)
            {
                case TraversalStrategy.PreOrder:
                case TraversalStrategy.PreOrderReverse:
                    _stack.Push(new StackEntry(_root, 0));
                    break;
                case TraversalStrategy.InOrder:
                case TraversalStrategy.InOrderReverse:
                    _inOrderCursor = _root;
                    _inOrderDepth = 0;
                    break;
                case TraversalStrategy.PostOrder:
                case TraversalStrategy.PostOrderReverse:
                    _stack.Push(new StackEntry(_root, 0, false));
                    break;
                default:
                    throw new NotSupportedException($"Reset is not supported for '{_strategy}'.");
            }
        }

        public void Dispose() { }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return new KeyValueIterator(InOrder());
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        if (!TryGetValue(item.Key, out var value)) { return false; }
        return EqualityComparer<TValue>.Default.Equals(value, item.Value);
    }
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null) { throw new ArgumentException("array is nil"); }
        if (arrayIndex < 0 || arrayIndex > array.Length) { throw new ArgumentOutOfRangeException(nameof(arrayIndex)); }
        if (array.Length - arrayIndex < Count) { throw new ArgumentException("array is too small"); }

        foreach (var e in InOrder())
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(e.Key, e.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    private struct KeyValueIterator(IEnumerable<TreeEntry<TKey, TValue>> source) :
        IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly IEnumerator<TreeEntry<TKey, TValue>> _inner = source.GetEnumerator();

        public KeyValuePair<TKey, TValue> Current { get; private set; } = default;
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (!_inner.MoveNext()) { return false; }
            var e = _inner.Current;
            Current = new KeyValuePair<TKey, TValue>(e.Key, e.Value);
            return true;
        }

        public void Reset()
        {
            _inner.Reset();
            Current = default;
        }

        public void Dispose() => _inner.Dispose();
    }
}