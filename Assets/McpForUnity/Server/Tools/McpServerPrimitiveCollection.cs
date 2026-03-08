using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ModelContextProtocol.Server
{
    public class McpServerPrimitiveCollection<T> : IReadOnlyList<T>
    {
        private readonly List<T> _items = new List<T>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private int _version;

        public event EventHandler<PrimitiveCollectionChangedEventArgs<T>> Changed;

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _items.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public T this[int index]
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _items[index];
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public void Add(T item)
        {
            Throw.IfNull(item);

            _lock.EnterWriteLock();
            try
            {
                _items.Add(item);
                _version++;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            OnChanged(new PrimitiveCollectionChangedEventArgs<T>(item, CollectionChangeAction.Add));
        }

        public bool Remove(T item)
        {
            bool removed;
            _lock.EnterWriteLock();
            try
            {
                removed = _items.Remove(item);
                if (removed)
                {
                    _version++;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            if (removed)
            {
                OnChanged(new PrimitiveCollectionChangedEventArgs<T>(item, CollectionChangeAction.Remove));
            }

            return removed;
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _items.Clear();
                _version++;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            OnChanged(new PrimitiveCollectionChangedEventArgs<T>(default, CollectionChangeAction.Reset));
        }

        protected virtual void OnChanged(PrimitiveCollectionChangedEventArgs<T> e)
        {
            Changed?.Invoke(this, e);
        }

        public IEnumerator<T> GetEnumerator()
        {
            _lock.EnterReadLock();
            try
            {
                var snapshot = _items.ToArray();
                return ((IEnumerable<T>)snapshot).GetEnumerator();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public enum CollectionChangeAction
    {
        Add,
        Remove,
        Reset
    }

    public class PrimitiveCollectionChangedEventArgs<T>
    {
        public T Item { get; }
        public CollectionChangeAction Action { get; }

        public PrimitiveCollectionChangedEventArgs(T item, CollectionChangeAction action)
        {
            Item = item;
            Action = action;
        }
    }
}
