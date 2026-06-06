using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract partial class Polymorph
{
    [System.Serializable]
    public class ListOf<T> : IList<T> where T : Polymorph
    {
        [SerializeField, SerializeReference]
        public List<T> items = new();


        // IList<T> implementation - delegate to the inner list.
        #region IList implementation
        public T this[int index]
        {
            get => items[index];
            set
            {
                var old = items[index];
                items[index] = value;
                OnRemoved(old, index);
                OnAdded(value, index);
            }
        }

        public int Count => items.Count;
        public bool IsReadOnly => ((ICollection<T>)items).IsReadOnly;

        public void Add(T item)
        {
            items.Add(item);
            OnAdded(item, items.Count - 1);
        }
        public void Clear()
        {
            for (int i = 0; i < items.Count; i++) OnRemoved(items[i], i);

            items.Clear();
            OnCleared();
        }
        public bool Contains(T item) => items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        public int IndexOf(T item) => items.IndexOf(item);
        public void Insert(int index, T item)
        {
            items.Insert(index, item);
            OnAdded(item, index);
        }

        public bool Remove(T item)
        {
            if (!items.Contains(item)) return false;
            int existingIndex = items.IndexOf(item);

            items.Remove(item);
            OnRemoved(item, existingIndex);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index > items.Count - 1) throw new ArgumentOutOfRangeException(nameof(index));
            T old = items[index];
            items.RemoveAt(index);
            OnRemoved(old, index);
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)items).GetEnumerator();
        #endregion

        protected virtual void OnAdded(T item, int id) { }
        protected virtual void OnRemoved(T item, int id) { }
        protected virtual void OnCleared() { }

    }

    [System.Serializable]
    public class UniqueList<T> : IList<T> where T : Polymorph
    {
        [SerializeField, SerializeReference]
        public List<T> items = new();

        // IList<T> implementation with uniqueness enforcement.
        #region IList implementation
        public T this[int index]
        {
            get => items[index];
            set
            {
                if (value != null)
                {
                    // Ensure no other slot contains the same runtime type.
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (i == index) continue;
                        var existing = items[i];
                        if (existing != null && existing.GetType() == value.GetType() && !ReferenceEquals(existing, value))
                            throw new InvalidOperationException($"Cannot add duplicate item of type '{value.GetType().Name}' to UniqueList.");
                    }
                }
                var old = items[index];
                items[index] = value;
                OnRemoved(old, index);
                OnAdded(value, index);
            }
        }
        public int Count => items.Count;
        public bool IsReadOnly => ((ICollection<T>)items).IsReadOnly;
        public void Add(T item)
        {
            if (item != null)
            {
                if (items.Any(e => e != null && e.GetType() == item.GetType() && !ReferenceEquals(e, item)))
                    throw new InvalidOperationException($"Cannot add duplicate item of type '{item.GetType().Name}' to UniqueList.");
            }
            items.Add(item);
            OnAdded(item, items.Count - 1);
        }
        public void Clear()
        {
            for (int i = 0; i < items.Count; i++)
                OnRemoved(items[i], i);

            items.Clear();
            OnCleared();
        }
        public bool Contains(T item) => items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        public int IndexOf(T item) => items.IndexOf(item);

        public void Insert(int index, T item)
        {
            if (item != null)
            {
                if (items.Any(e => e != null && e.GetType() == item.GetType() && !ReferenceEquals(e, item)))
                    throw new InvalidOperationException($"Cannot insert duplicate item of type '{item.GetType().Name}' to UniqueList.");
            }
            items.Insert(index, item);
            OnAdded(item, index);
        }
        public bool Remove(T item)
        {
            if (!items.Contains(item)) return false;
            int existingIndex = items.IndexOf(item);

            items.Remove(item);
            OnRemoved(item, existingIndex);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index > items.Count - 1) throw new ArgumentOutOfRangeException(nameof(index));
            T old = items[index];
            items.RemoveAt(index);
            OnRemoved(old, index);
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)items).GetEnumerator();
        #endregion

        // Additional dictionary-like and utility methods:

        /// <summary>
        /// Gets the value associated with the specified type.
        /// </summary>
        /// <param name="I">The type whose associated value to get.</param>
        /// <returns>The value associated with the specified type.</returns>
        public T this[Type I]
        {
            get
            {
                T found = items.FirstOrDefault(e => e.GetType() == I);
                return found;
            }
        }

        /// <summary>
        /// Returns the first stored element whose runtime Type equals the provided Type, or null if none.
        /// </summary>
        public T GetByType(Type type)
        {
            if (type == null) return null;
            return items.FirstOrDefault(e => e != null && e.GetType() == type);
        }

        /// <summary>
        /// Tries to get an element by runtime Type.
        /// </summary>
        public bool TryGetByType(Type type, out T value)
        {
            value = GetByType(type);
            return value != null;
        }

        /// <summary>
        /// Typed convenience getter. Returns the stored instance of U (or null).
        /// </summary>
        public U Get<U>() where U : T
        {
            var found = items.FirstOrDefault(e => e is U);
            return (U)found;
        }

        /// <summary>
        /// Typed try-get convenience.
        /// </summary>
        public bool TryGet<U>(out U value) where U : T
        {
            var found = items.FirstOrDefault(e => e is U);
            value = (U)found;
            return found != null;
        }

        /// <summary>
        /// Returns whether any element of the given runtime Type exists in the list.
        /// </summary>
        public bool ContainsType(Type type)
        {
            if (type == null) return false;
            return items.Any(e => e != null && e.GetType() == type);
        }

        /// <summary>
        /// Returns index of the element whose runtime Type equals the provided Type, or -1.
        /// </summary>
        public int IndexOfType(Type type)
        {
            if (type == null) return -1;
            for (int i = 0; i < items.Count; i++)
            {
                var e = items[i];
                if (e != null && e.GetType() == type) return i;
            }
            return -1;
        }

        /// <summary>
        /// Replace the existing element of the given runtime Type with 'item' or add it if missing.
        /// If 'item' is non-null its runtime type must match 'type'.
        /// </summary>
        public void SetByType(Type type, T item)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (item != null && item.GetType() != type) throw new ArgumentException("Item type does not match provided type.", nameof(item));

            int idx = IndexOfType(type);
            if (idx >= 0)
            {
                OnRemoved(item, idx);
                items[idx] = item;
                OnAdded(item, idx);
            }
            else Add(item);
        }

        protected virtual void OnAdded(T item, int id) { }
        protected virtual void OnRemoved(T item, int id) { }
        protected virtual void OnCleared() { }
    }

    public class Single<T> where T : Polymorph
    {
        [SerializeField, SerializeReference]
        private T value;

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                OnSet();
            }
        }

        public void Clear() => value = default;

        protected virtual void OnSet() { }

        public static implicit operator T(Single<T> slot) => slot != null ? slot.Value : default;
    }

    //In case of changes, use UnityEngine.Scripting.APIUpdating.MovedFromAttribute;
}
