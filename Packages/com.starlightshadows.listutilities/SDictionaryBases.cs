using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Windows;

namespace SLS.ListUtilities
{
    /// <summary>
    /// An abstract class that handles most of the basic functionality for a Serializable Dictionary.
    /// </summary>
    /// <typeparam name="TK"></typeparam>
    /// <typeparam name="TV"></typeparam>
    [Serializable]
    public abstract class SDictionaryAbstract<TK, TV> : ILookupTable,
        IDictionary, IDictionary<TK, TV>, IEnumerable<KeyValuePair<TK, TV>>,
        IReadOnlyList<TV>, IReadOnlyCollection<TV>
    {

        protected abstract List<TK> SerializedKeys { get; }
        protected abstract List<TV> SerializedValues { get; }


        public IReadOnlyList<TK> Keys => new List<TK>(SerializedKeys);
        public IReadOnlyList<TV> Values => new List<TV>(SerializedValues);
        public IReadOnlyList<KeyValuePair<TK, TV>> KeyValuePairs
        {
            get
            {
                List<TK> keys = new();
                List<KeyValuePair<TK, TV>> result = new();
                for (int i = 0; i < SerializedKeys.Count; i++)
                {
                    if (!keys.Contains(SerializedKeys[i]))
                    {
                        result.Add(new(SerializedKeys[i], SerializedValues[i]));
                        keys.Add(SerializedKeys[i]);
                    }
                }
                return result;
            }
        }

        public TV Get(TK key) => SerializedKeys.Contains(key) ? SerializedValues[SerializedKeys.IndexOf(key)] : default;

        public bool TryGet(TK key, out TV result)
        {
            result = default;
            if (!SerializedKeys.Contains(key)) return false;
            result = SerializedValues[SerializedKeys.IndexOf(key)];
            return true;
        }

        public TV this[TK key]
        {
            get => Get(key);
            set
            {
                if (IsReadOnly) return;
                if (SerializedKeys.Contains(key))
                    SerializedValues[SerializedKeys.IndexOf(key)] = value;
                else Add(key, value);
            }
        }

        public int Count => SerializedValues.Count;
        public bool IsReadOnly { get; protected set; }

        public virtual void Add(TK key, TV value)
        {
            if (IsReadOnly) return;
            if (SerializedKeys.Contains(key)) return;
            SerializedKeys.Add(key);
            SerializedValues.Add(value);
        }
        public void Add(KeyValuePair<TK, TV> item) => Add(item.Key, item.Value);
        protected virtual void OnAddKeyAndValue() { }

        public void Remove(TK key)
        {
            if (IsReadOnly || !SerializedKeys.Contains(key)) return;
            RemoveAt(IndexOf(key));
        }
        public void Remove(TV val)
        {
            if (IsReadOnly || !SerializedValues.Contains(val)) return;
            RemoveAt(IndexOf(val));
        }
        public virtual void RemoveAt(int i)
        {
            if (IsReadOnly || i < 0 || i >= SerializedValues.Count) return;
            SerializedKeys.RemoveAt(i);
            SerializedValues.RemoveAt(i);
        }
        public virtual void Clear()
        {
            SerializedKeys.Clear();
            SerializedValues.Clear();
        }

        public bool ContainsKey(TK i) => SerializedKeys.Contains(i);
        public bool ContainsValue(TV i) => SerializedValues.Contains(i);
        public bool ContainsPair(KeyValuePair<TK, TV> test) => ContainsKey(test.Key) && this[test.Key].Equals(test.Value);
        public bool Contains(TK i) => ContainsKey(i);
        public bool Contains(TV i) => ContainsValue(i);
        public bool Contains(KeyValuePair<TK, TV> test) => ContainsPair(test);

        public int IndexOfKey(TK i) => SerializedKeys.IndexOf(i);
        public int IndexOfValue(TV i) => SerializedValues.IndexOf(i);
        public int IndexOf(TK i) => IndexOfKey(i);
        public int IndexOf(TV i) => IndexOfValue(i);

        public Dictionary<TK, TV> ToNativeDictionary() => SerializedKeys.Zip(SerializedValues, (n, v) => new { n, v }).ToDictionary(x => x.n, x => x.v);

        public TK KeyFromIndex(int i) => SerializedKeys[i];
        public TV ValueFromIndex(int i) => SerializedValues[i];

        public List<bool> Duplicates()
        {
            List<TK> firstOccurences = new();
            List<bool> DuplicateValues = Enumerable.Repeat(false, SerializedKeys.Count).ToList();
            for (int i = 0; i < SerializedKeys.Count; i++)
            {
                if (!firstOccurences.Contains(SerializedKeys[i]))
                {
                    firstOccurences.Add(SerializedKeys[i]);
                    DuplicateValues[i] = false;
                }
                else
                {
                    DuplicateValues[i] = true;
                }
            }
            return DuplicateValues;
        }
        public void RemoveDuplicates()
        {
            List<TK> firstOccurences = new();
            for (int i = 0; i < SerializedKeys.Count; i++)
            {
                if (!firstOccurences.Contains(SerializedKeys[i]))
                    firstOccurences.Add(SerializedKeys[i]);
                else
                {
                    RemoveAt(i);
                    i--;
                }
            }
        }

        #region Interface
        ICollection IDictionary.Keys => Keys as ICollection;
        ICollection IDictionary.Values => Values as ICollection;
        ICollection<TK> IDictionary<TK, TV>.Keys => Keys as ICollection<TK>;
        ICollection<TV> IDictionary<TK, TV>.Values => Values as ICollection<TV>;
        int ICollection<KeyValuePair<TK, TV>>.Count => Count;

        public bool IsFixedSize => false;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        TV IReadOnlyList<TV>.this[int index] => ((IReadOnlyList<TV>)SerializedValues)[index];

        object IDictionary.this[object key]
        {
            get => this[(TK)key];
            set => this[(TK)key] = (TV)value;
        }

        void IDictionary.Add(object key, object value) => Add((TK)key, (TV)value);
        bool IDictionary.Contains(object key) => ContainsKey((TK)key);
        IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumeratorAdapter(KeyValuePairs);
        void IDictionary.Remove(object key) => Remove((TK)key);
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TK, TV>>)this).GetEnumerator();
        bool IDictionary<TK, TV>.Remove(TK key)
        {
            if (!ContainsKey(key)) return false;
            Remove(key);
            return true;
        }
        bool IDictionary<TK, TV>.TryGetValue(TK key, out TV value) => TryGet(key, out value);
        void ICollection<KeyValuePair<TK, TV>>.CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentException("Destination array is not long enough.", nameof(array));
            int i = arrayIndex;
            foreach (var kv in KeyValuePairs)
                array[i++] = kv;
        }
        bool ICollection<KeyValuePair<TK, TV>>.Remove(KeyValuePair<TK, TV> item)
        {
            if (!ContainsPair(item)) return false;
            Remove(item.Key);
            return true;
        }
        IEnumerator<KeyValuePair<TK, TV>> IEnumerable<KeyValuePair<TK, TV>>.GetEnumerator() => KeyValuePairs.GetEnumerator();
        public void CopyTo(Array array, int index)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (array.Rank != 1) throw new ArgumentException("Only single dimensional arrays are supported.", nameof(array));
            if (array.Length - index < Count) throw new ArgumentException("Destination array is not long enough.", nameof(array));

            if (array is DictionaryEntry[] deArray)
            {
                int i = index;
                foreach (var kv in KeyValuePairs)
                {
                    deArray[i++] = new DictionaryEntry(kv.Key, kv.Value);
                }
                return;
            }

            if (array is KeyValuePair<TK, TV>[] kvArray)
            {
                ((ICollection<KeyValuePair<TK, TV>>)this).CopyTo(kvArray, index);
                return;
            }

            if (array is object[] objArray)
            {
                int i = index;
                foreach (var kv in KeyValuePairs)
                    objArray[i++] = kv;
                return;
            }

            throw new ArgumentException("Invalid array type", nameof(array));
        }
        public IEnumerator<TV> GetEnumerator() => ((IEnumerable<TV>)SerializedValues).GetEnumerator();

        // Adapter to expose a IDictionaryEnumerator from an IEnumerable<KeyValuePair<,>>
        private class DictionaryEnumeratorAdapter : IDictionaryEnumerator
        {
            private readonly IEnumerator<KeyValuePair<TK, TV>> _enumerator;

            public DictionaryEnumeratorAdapter(IEnumerable<KeyValuePair<TK, TV>> source)
            {
                _enumerator = source.GetEnumerator();
            }

            public DictionaryEntry Entry => new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value);
            public object Key => _enumerator.Current.Key;
            public object Value => _enumerator.Current.Value;
            public object Current => Entry;
            public bool MoveNext() => _enumerator.MoveNext();
            public void Reset() => _enumerator.Reset();
        }
        #endregion

    }

    /// <summary>
    /// Like a <see cref="KeyValuePair"/> but with a string Name and a int Hash.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public struct NameHashValueTrio<T>
    {
        public NameHashValueTrio(string name, T value)
        {
            Name = name;
            Hash = name.Hash();
            Value = value;
        }
        public NameHashValueTrio(int hash, T value)
        {
            Name = null;
            Hash = hash;
            Value = value;
        }
        public string Name { get; private set; }
        public int Hash { get; private set; }
        public T Value { get; private set; }
    }

    /// <summary>
    /// An interface for Dictionary types that provides a means of Checking/Removing what values have a duplicate KEY.
    /// </summary>
    public interface ILookupTable
    {
        public List<bool> Duplicates();
        public void RemoveDuplicates();
    }
}
