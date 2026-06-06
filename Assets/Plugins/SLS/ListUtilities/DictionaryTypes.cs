using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SLS.ListUtilities
{
    /// <summary>
    /// A Serializable Dictionary
    /// </summary>
    /// <typeparam name="TK"></typeparam>
    /// <typeparam name="TV"></typeparam>
    [Serializable]
    public class DictionaryS<TK, TV> : SDictionaryAbstract<TK, TV>
    {
        protected override List<TK> SerializedKeys => serializedKeys;
        [SerializeField] protected List<TK> serializedKeys = new();
        protected override List<TV> SerializedValues => serializedValues;
        [SerializeField] protected List<TV> serializedValues = new();
    }
    /// <summary>
    /// A Serializable Dictionary that uses <see cref="SerializeReference"/> on the Values.
    /// </summary>
    /// <typeparam name="TK"></typeparam>
    /// <typeparam name="TV"></typeparam>
    [Serializable]
    public class DictionarySReference<TK, TV> : SDictionaryAbstract<TK, TV>
    {
        protected override List<TK> SerializedKeys => serializedKeys;
        [SerializeField] protected List<TK> serializedKeys = new();
        protected override List<TV> SerializedValues => serializedValues;
        [SerializeField, SerializeReference] protected List<TV> serializedValues = new();

    }

    /// <summary>
    /// Like a Serialized Dictionary, but stores both a name and an integer hash for even faster looking up of the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class HashedListS<T> : SDictionaryAbstract<int, T>
    {
        [SerializeField] List<string> SerializedNames => serializedNames;
        [SerializeField] protected List<string> serializedNames = new();

        protected override List<int> SerializedKeys => serializedKeys;
        [SerializeField] protected List<int> serializedKeys = new();
        protected override List<T> SerializedValues => serializedValues;
        [SerializeField] protected List<T> serializedValues = new();

        public IReadOnlyList<string> Names => new List<string>(SerializedNames);

        public T Get(string name, bool USENAME = false)
        {
            if (!USENAME)
            {
                int hash = name.GetHashCode();
                return SerializedKeys.Contains(hash) ? SerializedValues[SerializedKeys.IndexOf(hash)] : default;
            }
            else
            {
                return SerializedNames.Contains(name) ? SerializedValues[SerializedNames.IndexOf(name)] : default;
            }
        }

        public bool TryGet(string name, out T result, bool USENAME = false)
        {
            result = default;
            if (!USENAME)
            {
                int hash = name.GetHashCode();
                if (!SerializedKeys.Contains(hash)) return false;
                result = SerializedValues[SerializedKeys.IndexOf(hash)];
                return true;
            }
            else
            {
                if (!SerializedNames.Contains(name)) return false;
                result = SerializedValues[SerializedNames.IndexOf(name)];
                return true;
            }
        }

        public T this[string name]
        {
            get => Get(name);
            set
            {
                if (IsReadOnly) return;
                int hash = name.GetHashCode();
                if (SerializedKeys.Contains(hash))
                    SerializedValues[SerializedKeys.IndexOf(hash)] = value;
                else
                {
                    SerializedNames.Add(name);
                    SerializedKeys.Add(hash);
                    SerializedValues.Add(value);
                }
            }
        }

        public void Add(string name, T value)
        {
            if (IsReadOnly) return;
            int hash = name.GetHashCode();
            if (SerializedKeys.Contains(hash)) return;
            SerializedNames.Add(name);
            SerializedKeys.Add(hash);
            SerializedValues.Add(value);
        }
        protected override void OnAddKeyAndValue()
        {
            SerializedNames.Add(SerializedKeys[^1].ToString());
        }
        public void Add(T value)
        {
            if (IsReadOnly) return;
            SerializedKeys.Add(Guid.NewGuid().ToString().GetHashCode());
            SerializedNames.Add(SerializedKeys[^1].ToString());
            SerializedValues.Add(value);
        }
        public void Add(KeyValuePair<string, T> item) => Add(item.Key, item.Value);

        public void Remove(string name)
        {
            if (IsReadOnly || !SerializedNames.Contains(name)) return;
            RemoveAt(IndexOf(name));
        }
        public override void RemoveAt(int i)
        {
            if (IsReadOnly || i < 0 || i >= SerializedValues.Count) return;
            SerializedNames.RemoveAt(i);
            SerializedKeys.RemoveAt(i);
            SerializedValues.RemoveAt(i);
        }
        public override void Clear()
        {
            SerializedNames.Clear();
            SerializedKeys.Clear();
            SerializedValues.Clear();
        }

        public bool ContainsName(string i) => SerializedNames.Contains(i);
        public bool Contains(string i) => ContainsName(i);

        public int IndexOfName(string i) => SerializedNames.IndexOf(i);
        public int IndexOf(string i) => IndexOfName(i);

        public Dictionary<string, T> ToNameDictionary() => SerializedNames.Zip(SerializedValues, (n, v) => new { n, v }).ToDictionary(x => x.n, x => x.v);
        public Dictionary<int, T> ToKeyDictionary() => ToNativeDictionary();
        public Dictionary<string, int> ToHashDictionary() => SerializedNames.Zip(SerializedKeys, (n, k) => new { n, k }).ToDictionary(x => x.n, x => x.k);

        public string NameFromIndex(int i) => SerializedNames[i];
    }

    /// <summary>
    /// Like a Serialized Dictionary (using <see cref="SerializeReference"/>), but stores both a name and an integer hash for even faster looking up of the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class HashedListSReference<T> : SDictionaryAbstract<int, T>
    {
        [SerializeField] List<string> SerializedNames => serializedNames;
        [SerializeField] protected List<string> serializedNames = new();

        protected override List<int> SerializedKeys => serializedKeys;
        [SerializeField] protected List<int> serializedKeys = new();
        protected override List<T> SerializedValues => serializedValues;
        [SerializeField, SerializeReference] protected List<T> serializedValues = new();

        public IReadOnlyList<string> Names => new List<string>(SerializedNames);

        public T Get(string name, bool USENAME = false)
        {
            if (!USENAME)
            {
                int hash = name.GetHashCode();
                return SerializedKeys.Contains(hash) ? SerializedValues[SerializedKeys.IndexOf(hash)] : default;
            }
            else
            {
                return SerializedNames.Contains(name) ? SerializedValues[SerializedNames.IndexOf(name)] : default;
            }
        }

        public bool TryGet(string name, out T result, bool USENAME = false)
        {
            result = default;
            if (!USENAME)
            {
                int hash = name.GetHashCode();
                if (!SerializedKeys.Contains(hash)) return false;
                result = SerializedValues[SerializedKeys.IndexOf(hash)];
                return true;
            }
            else
            {
                if (!SerializedNames.Contains(name)) return false;
                result = SerializedValues[SerializedNames.IndexOf(name)];
                return true;
            }
        }

        public T this[string name]
        {
            get => Get(name);
            set
            {
                if (IsReadOnly) return;
                int hash = name.GetHashCode();
                if (SerializedKeys.Contains(hash))
                    SerializedValues[SerializedKeys.IndexOf(hash)] = value;
                else
                {
                    SerializedNames.Add(name);
                    SerializedKeys.Add(hash);
                    SerializedValues.Add(value);
                }
            }
        }

        public void Add(string name, T value)
        {
            if (IsReadOnly) return;
            int hash = name.GetHashCode();
            if (SerializedKeys.Contains(hash)) return;
            SerializedNames.Add(name);
            SerializedKeys.Add(hash);
            SerializedValues.Add(value);
        }
        protected override void OnAddKeyAndValue()
        {
            SerializedNames.Add(SerializedKeys[^1].ToString());
        }
        public void Add(T value)
        {
            if (IsReadOnly) return;
            SerializedKeys.Add(Guid.NewGuid().ToString().GetHashCode());
            SerializedNames.Add(SerializedKeys[^1].ToString());
            SerializedValues.Add(value);
        }
        public void Add(KeyValuePair<string, T> item) => Add(item.Key, item.Value);

        public void Remove(string name)
        {
            if (IsReadOnly || !SerializedNames.Contains(name)) return;
            RemoveAt(IndexOf(name));
        }
        public override void RemoveAt(int i)
        {
            if (IsReadOnly || i < 0 || i >= SerializedValues.Count) return;
            SerializedNames.RemoveAt(i);
            SerializedKeys.RemoveAt(i);
            SerializedValues.RemoveAt(i);
        }
        public override void Clear()
        {
            SerializedNames.Clear();
            SerializedKeys.Clear();
            SerializedValues.Clear();
        }

        public bool ContainsName(string i) => SerializedNames.Contains(i);
        public bool Contains(string i) => ContainsName(i);

        public int IndexOfName(string i) => SerializedNames.IndexOf(i);
        public int IndexOf(string i) => IndexOfName(i);

        public Dictionary<string, T> ToNameDictionary() => SerializedNames.Zip(SerializedValues, (n, v) => new { n, v }).ToDictionary(x => x.n, x => x.v);
        public Dictionary<int, T> ToKeyDictionary() => ToNativeDictionary();
        public Dictionary<string, int> ToHashDictionary() => SerializedNames.Zip(SerializedKeys, (n, k) => new { n, k }).ToDictionary(x => x.n, x => x.k);
    }
}
