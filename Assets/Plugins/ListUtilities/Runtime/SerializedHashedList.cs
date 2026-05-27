using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ListUtilities
{
    /// <summary>
    /// Like a Dictionary, but stores both a name and an integer hash for even faster looking up of the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Serializable]
    public class SerializedHashedList<T> : IHashedListPreGeneric
    {
        [SerializeField] List<string> SerializedNames;
        [SerializeField] List<int> SerializedKeys;
        [SerializeField] List<T> SerializedValues;

        public SerializedHashedList()
        {
            SerializedNames = new List<string>();
            SerializedKeys = new List<int>();
            SerializedValues = new List<T>();
        }




        public IReadOnlyList<string> Names => new List<string>(SerializedNames);
        public IReadOnlyList<int> Keys => new List<int>(SerializedKeys);
        public IReadOnlyList<T> Values => new List<T>(SerializedValues);

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
        public T Get(int key) => SerializedKeys.Contains(key) ? SerializedValues[SerializedKeys.IndexOf(key)] : default;

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
        public bool TryGet(int key, out T result)
        {
            result = default;
            if (!SerializedKeys.Contains(key)) return false;
            result = SerializedValues[SerializedKeys.IndexOf(key)];
            return true;
        }


        public T this[int key]
        {
            get => Get(key);
            set
            {
                if (IsReadOnly) return;
                if (SerializedKeys.Contains(key))
                {
                    int i = SerializedKeys.IndexOf(key);
                    SerializedValues[i] = value;
                }
                else
                {
                    SerializedNames.Add(key.ToString());
                    SerializedKeys.Add(key);
                    SerializedValues.Add(value);
                }
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
                {
                    SerializedValues[SerializedKeys.IndexOf(hash)] = value;
                }
                else
                {
                    SerializedNames.Add(name);
                    SerializedKeys.Add(hash);
                    SerializedValues.Add(value);
                }
            }
        }

        public int Count => SerializedValues.Count;
        public readonly bool IsReadOnly;

        public void Add(string name, T value)
        {
            if (IsReadOnly) return;
            int hash = name.GetHashCode();
            if (SerializedKeys.Contains(hash)) return;
            SerializedNames.Add(name);
            SerializedKeys.Add(hash);
            SerializedValues.Add(value);
        }
        public void Add(int key, T value)
        {
            if (IsReadOnly) return;
            if (SerializedKeys.Contains(key)) return;
            SerializedNames.Add(key.ToString());
            SerializedKeys.Add(key);
            SerializedValues.Add(value);
        }
        public void Add(T value)
        {
            if (IsReadOnly) return;
            SerializedKeys.Add(Guid.NewGuid().ToString().GetHashCode());
            SerializedNames.Add(SerializedKeys[^1].ToString());
            SerializedValues.Add(value);
        }
        public void Add(KeyValuePair<string, T> item) => Add(item.Key, item.Value);
        public void Add(KeyValuePair<int, T> item) => Add(item.Key, item.Value);

        public void Remove(string name)
        {
            if (IsReadOnly || !SerializedNames.Contains(name)) return;
            int i = SerializedNames.IndexOf(name);
            SerializedNames.RemoveAt(i);
            SerializedKeys.RemoveAt(i);
            SerializedValues.RemoveAt(i);
        }
        public void Remove(int key)
        {
            if (IsReadOnly || !SerializedKeys.Contains(key)) return;
            int i = SerializedKeys.IndexOf(key);
            SerializedNames.RemoveAt(i);
            SerializedKeys.RemoveAt(i);
            SerializedValues.RemoveAt(i);
        }
        public void Remove(T val)
        {
            if (IsReadOnly || !SerializedValues.Contains(val)) return;
            int i = SerializedValues.IndexOf(val);
            SerializedNames.RemoveAt(i);
            SerializedKeys.RemoveAt(i);
            SerializedValues.RemoveAt(i);
        }
        public void RemoveAt(int i)
        {
            if (IsReadOnly || i < 0 || i >= SerializedValues.Count) return;
            SerializedNames.RemoveAt(i);
            SerializedKeys.RemoveAt(i);
            SerializedValues.RemoveAt(i);
        }
        public void Clear()
        {
            SerializedNames.Clear();
            SerializedKeys.Clear();
            SerializedValues.Clear();
        }

        public bool ContainsName(string i) => SerializedNames.Contains(i);
        public bool ContainsKey(int i) => SerializedKeys.Contains(i);
        public bool ContainsValue(T i) => SerializedValues.Contains(i);
        public bool Contains(string i) => ContainsName(i);
        public bool Contains(int i) => ContainsKey(i);
        public bool Contains(T i) => ContainsValue(i);

        public int IndexOfName(string i) => SerializedNames.IndexOf(i);
        public int IndexOfKey(int i) => SerializedKeys.IndexOf(i);
        public int IndexOfValue(T i) => SerializedValues.IndexOf(i);
        public int IndexOf(string i) => IndexOfName(i);
        public int IndexOf(int i) => IndexOfKey(i);
        public int IndexOf(T i) => IndexOfValue(i);

        public Dictionary<string, T> ToNameDictionary() => SerializedNames.Zip(SerializedValues, (n, v) => new { n, v }).ToDictionary(x => x.n, x => x.v);
        public Dictionary<int, T> ToKeyDictionary() => SerializedKeys.Zip(SerializedValues, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
        public Dictionary<string, int> ToHashDictionary() => SerializedNames.Zip(SerializedKeys, (n, k) => new { n, k }).ToDictionary(x => x.n, x => x.k);


        public List<bool> Duplicates()
        {
            List<int> firstOccurences = new();
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
            List<int> firstOccurences = new();
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

    }

    public interface IHashedListPreGeneric
    {
        public List<bool> Duplicates();
        public void RemoveDuplicates();
    }
}
