using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Axion.Core.Processing.Syntactic.Expressions;
using Axion.Core.Processing.Traversal;

namespace Axion.Core.Processing.Syntactic {
    /// <summary>
    ///     A special implementation of List that can handle expressions.
    ///     It can automatically bind parent of items
    ///     to one defined on list construction,
    ///     and provide some other useful methods.
    /// </summary>
    public class NodeList<T> : IList<T>
        where T : Expr {
        private Expr? parent;

        public Expr? Parent {
            get => parent;
            set {
                parent = value;
                if (parent == null || items == null) {
                    return;
                }
                foreach (T item in items) {
                    if (item != null) {
                        item.Parent = parent;
                    }
                }
            }
        }

        private readonly IList<T> items;

        void IList<T>.RemoveAt(int index) {
            items.RemoveAt(index);
        }

        public T this[int i] {
            get => items[i];
            set {
                value.Parent = Parent;
                value.Path   = new NodeListTreePath<T>(this, i);
                items[i]     = value;
            }
        }

        public bool Remove(T item) {
            return items.Remove(item);
        }

        public int  Count      => items.Count;
        public bool IsReadOnly => false;

        public T First {
            get =>
                items.Count > 0 ? items[0] : throw new IndexOutOfRangeException();
            set {
                if (items.Count > 0) {
                    items[0] = value;
                }
                else {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        public T Last {
            get =>
                items.Count > 0 ? items[^1] : throw new IndexOutOfRangeException();
            set {
                if (items.Count > 0) {
                    items[^1] = value;
                }
                else {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        internal static NodeList<T> From(Expr parent, IEnumerable<T>? collection) {
            if (collection == null) {
                return new NodeList<T>(parent);
            }
            if (collection is List<T> list) {
                return new NodeList<T>(parent, list);
            }
            return new NodeList<T>(parent, collection.ToList());
        }

        internal static NodeList<Expr> From(params Expr[] collection) {
            if (collection == null) {
                return new NodeList<Expr>(null);
            }
            return new NodeList<Expr>(null, collection.ToList());
        }

        internal static NodeList<T> From(params T[] collection) {
            if (collection == null) {
                return new NodeList<T>(null);
            }
            return new NodeList<T>(null, collection.ToList());
        }

        internal NodeList(Expr? parent) {
            Parent = parent;
            items  = new List<T>();
        }

        private NodeList(Expr? parent, IList<T> collection) {
            Parent = parent;
            items  = collection;
        }

        public void Insert(int index, T item) {
            item.Parent = Parent;
            item.Path   = new NodeListTreePath<T>(this, index);
            if (index == Count) {
                items.Add(item);
            }
            else {
                items.Insert(index, item);
                for (int i = index; i < Count; i++) {
                    ((NodeListTreePath<T>) items[i].Path).IndexInList = i;
                }
            }
        }

        public void Add(T item) {
            item.Path   = new NodeListTreePath<T>(this, Count);
            item.Parent = Parent;
            items.Add(item);
        }

        public int IndexOf(T item) {
            return items.IndexOf(item);
        }

        public bool Contains(T item) {
            return items.Contains(item);
        }

        public void Clear() {
            items.Clear();
        }

        public IEnumerator<T> GetEnumerator() {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        [Obsolete]
        void IList<T>.Insert(int index, T item) {
            throw new NotSupportedException();
        }

        [Obsolete]
        void ICollection<T>.Add(T item) {
            Add(item);
        }

        [Obsolete]
        public void CopyTo(T[] array, int arrayIndex) {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }
            if (arrayIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            if (array.Rank > 1) {
                throw new ArgumentException(
                    "Only single dimensional arrays are supported for the requested action.",
                    nameof(array)
                );
            }
            if (array.Length - arrayIndex < Count) {
                throw new ArgumentException(
                    "Not enough elements after index in the destination array."
                );
            }

            for (var i = 0; i < Count; i++) {
                array.SetValue(this[i], i + arrayIndex);
            }
        }
    }
}
