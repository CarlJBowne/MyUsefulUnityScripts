using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

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

        public void Clone(DictionaryS<TK, TV> source, DictionaryCloneOp op = DictionaryCloneOp.Transfer)
        {
            if (source == null) return;
            if (Count == 0) op = DictionaryCloneOp.TransferAndAdd;
            if (op is DictionaryCloneOp.ReplaceEntirely) Clear();
            for (int i = 0; i < source; i++)
                if (serializedKeys.Contains(source.serializedKeys[i]) || op is not DictionaryCloneOp.Transfer)
                    this[source.serializedKeys[i]] = source.serializedValues[i];
        }
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

        public void Clone(DictionarySReference<TK, TV> source, DictionaryCloneOp op = DictionaryCloneOp.Transfer)
        {
            if (source == null) return;
            if (Count == 0) op = DictionaryCloneOp.TransferAndAdd;
            if (op is DictionaryCloneOp.ReplaceEntirely) Clear();
            for (int i = 0; i < source; i++)
                if (serializedKeys.Contains(source.serializedKeys[i]) || op is not DictionaryCloneOp.Transfer)
                    this[source.serializedKeys[i]] = source.serializedValues[i];
        }
    }

    /// <summary>
    /// Like a Serialized Dictionary, but stores both a name and an integer hash for even faster looking up of the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class HashedListS<T> : SDictionaryAbstract<string, T>
    {
        [SerializeField, FormerlySerializedAs("serializedKeys")] List<int> serializedHashes;
        [SerializeField, FormerlySerializedAs("serializedNames")] List<string> serializedKeys;
        [SerializeField] List<T> serializedValues;

        protected override List<string> SerializedKeys => serializedKeys;
        protected override List<T> SerializedValues => serializedValues;
        protected virtual List<int> SerializedHashes => serializedHashes;

        public IReadOnlyList<int> Hashes => SerializedHashes;


        public T Get(string name, bool DoHash = false)
        {
            if (!DoHash)
            {
                return ContainsKey(name)
                    ? SerializedValues[SerializedKeys.IndexOf(name)]
                    : default;
            }
            else
            {
                int hash = name.Hash();
                return SerializedHashes.Contains(hash)
                    ? SerializedValues[SerializedHashes.IndexOf(hash)]
                    : default;
            }
        }

        public bool TryGet(string name, out T result, bool DoHash = false)
        {
            result = default;
            if (!DoHash)
            {
                if (ContainsKey(name))
                {
                    result = SerializedValues[SerializedKeys.IndexOf(name)];
                    return true;
                }
                else return false;
            }
            else
            {
                int hash = name.Hash();
                if (ContainsHash(hash))
                {
                    result = SerializedValues[SerializedHashes.IndexOf(hash)];
                    return true;
                }
                else return false;
            }
        }

        new public T this[string name]
        {
            get => Get(name);
            set
            {
                if (IsReadOnly) return;
                if (SerializedKeys.Contains(name))
                    SerializedValues[SerializedKeys.IndexOf(name)] = value;
                else
                {
                    SerializedKeys.Add(name);
                    // keep hash list in sync
                    serializedHashes.Add(name.Hash());
                    SerializedValues.Add(value);
                }
            }
        }

        public T Get(int hash)
        {
            return SerializedHashes.Contains(hash)
                    ? SerializedValues[SerializedHashes.IndexOf(hash)]
                    : default;
        }

        public bool TryGet(int hash, out T result)
        {
            result = default;
            if (ContainsHash(hash))
            {
                result = SerializedValues[SerializedHashes.IndexOf(hash)];
                return true;
            }
            else return false;
        }

        public T this[int hash]
        {
            get => Get(hash);
            set
            {
                if (IsReadOnly) return;
                if (SerializedHashes.Contains(hash))
                    SerializedValues[SerializedHashes.IndexOf(hash)] = value;
                else
                {
                    SerializedKeys.Add("HASHED_ONLY_ITEM");
                    // keep hash list in sync
                    serializedHashes.Add(hash);
                    SerializedValues.Add(value);
                }
            }
        }


        public override void Add(string name, T value)
        {
            if (IsReadOnly) return;
            if (SerializedKeys.Contains(name)) return;
            SerializedKeys.Add(name);
            SerializedHashes.Add(name.Hash());
            SerializedValues.Add(value);
        }
        protected override void OnAddKeyAndValue()
        {
            SerializedHashes.Add(SerializedKeys[^1].Hash());
        }
        public void Add(T value)
        {
            if (IsReadOnly) return;
            Guid G = Guid.NewGuid();
            SerializedKeys.Add(G.ToString());
            SerializedHashes.Add(G.ToString().Hash());
            SerializedValues.Add(value);
        }
        public override void Add(KeyValuePair<string, T> item) => Add(item.Key, item.Value);

        public override void Remove(string name)
        {
            if (IsReadOnly || !SerializedKeys.Contains(name)) return;
            RemoveAt(IndexOf(name));
        }
        public override void RemoveAt(int i)
        {
            if (IsReadOnly || i < 0 || i >= SerializedValues.Count) return;
            SerializedKeys.RemoveAt(i);
            SerializedHashes.RemoveAt(i);
            SerializedValues.RemoveAt(i);
        }
        public override void Clear()
        {
            SerializedKeys.Clear();
            SerializedHashes.Clear();
            SerializedValues.Clear();
        }


        public bool ContainsHash(int i) => SerializedHashes.Contains(i);
        public bool Contains(int i) => ContainsHash(i);

        public int IndexOfHash(int i) => SerializedHashes.IndexOf(i);
        public int IndexOf(int i) => IndexOfHash(i);

        public Dictionary<string, T> ToNameDictionary() => SerializedKeys.Zip(SerializedValues, (n, v) => new { n, v }).ToDictionary(x => x.n, x => x.v);
        public Dictionary<string, T> ToKeyDictionary() => ToNativeDictionary();
        public Dictionary<string, int> ToHashDictionary() => SerializedKeys.Zip(serializedHashes, (n, k) => new { n, k }).ToDictionary(x => x.n, x => x.k);

        // Access to secondary integer hashes
        public IReadOnlyList<int> Hash => serializedHashes;
        public int HashOf(string name) => !SerializedKeys.Contains(name)
            ? name.Hash()
            : SerializedHashes[SerializedKeys.IndexOf(name)];
        public bool TryGetHash(string name, out int hash)
        {
            hash = default;
            if (!SerializedKeys.Contains(name)) return false;
            hash = serializedHashes[SerializedKeys.IndexOf(name)];
            return true;
        }
        public void Clone(HashedListS<T> source, DictionaryCloneOp op = DictionaryCloneOp.Transfer)
        {
            if (source == null) return;
            if (Count == 0) op = DictionaryCloneOp.TransferAndAdd;
            if (op is DictionaryCloneOp.ReplaceEntirely) Clear();
            for (int i = 0; i < source; i++)
                if (SerializedHashes.Contains(source.SerializedHashes[i]) || op is not DictionaryCloneOp.Transfer)
                    this[source.SerializedKeys[i]] = source.SerializedValues[i];
        }
    }

    /// <summary>
    /// Like a Serialized Dictionary (using <see cref="SerializeReference"/>), but stores both a name and an integer hash for even faster looking up of the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class HashedListSReference<T> : SDictionaryAbstract<string, T>
    {
        [SerializeField, FormerlySerializedAs("serializedNames")] List<string> serializedKeys;
        [SerializeField, FormerlySerializedAs("serializedKeys")] List<int> serializedHashes;
        [SerializeField, SerializeReference] List<T> serializedValues;

        protected override List<string> SerializedKeys => serializedKeys;
        protected override List<T> SerializedValues => serializedValues;
        protected virtual List<int> SerializedHashes => serializedHashes;

        public IReadOnlyList<int> Hashes => SerializedHashes;


        public T Get(string name, bool DoHash = false)
        {
            if (!DoHash)
            {
                return ContainsKey(name)
                    ? SerializedValues[SerializedKeys.IndexOf(name)]
                    : default;
            }
            else
            {
                int hash = name.Hash();
                return SerializedHashes.Contains(hash)
                    ? SerializedValues[SerializedHashes.IndexOf(hash)]
                    : default;
            }
        }

        public bool TryGet(string name, out T result, bool DoHash = false)
        {
            result = default;
            if (!DoHash)
            {
                if (ContainsKey(name))
                {
                    result = SerializedValues[SerializedKeys.IndexOf(name)];
                    return true;
                }
                else return false;
            }
            else
            {
                int hash = name.Hash();
                if (ContainsHash(hash))
                {
                    result = SerializedValues[SerializedHashes.IndexOf(hash)];
                    return true;
                }
                else return false;
            }
        }

        new public T this[string name]
        {
            get => Get(name);
            set
            {
                if (IsReadOnly) return;
                if (SerializedKeys.Contains(name))
                    SerializedValues[SerializedKeys.IndexOf(name)] = value;
                else
                {
                    SerializedKeys.Add(name);
                    // keep hash list in sync
                    serializedHashes.Add(name.Hash());
                    SerializedValues.Add(value);
                }
            }
        }

        public T Get(int hash)
        {
            return SerializedHashes.Contains(hash)
                    ? SerializedValues[SerializedHashes.IndexOf(hash)]
                    : default;
        }

        public bool TryGet(int hash, out T result)
        {
            result = default;
            if (ContainsHash(hash))
            {
                result = SerializedValues[SerializedHashes.IndexOf(hash)];
                return true;
            }
            else return false;
        }

        public T this[int hash]
        {
            get => Get(hash);
            set
            {
                if (IsReadOnly) return;
                if (SerializedHashes.Contains(hash))
                    SerializedValues[SerializedHashes.IndexOf(hash)] = value;
                else
                {
                    SerializedKeys.Add("HASHED_ONLY_ITEM");
                    // keep hash list in sync
                    serializedHashes.Add(hash);
                    SerializedValues.Add(value);
                }
            }
        }


        public override void Add(string name, T value)
        {
            if (IsReadOnly) return;
            if (SerializedKeys.Contains(name)) return;
            SerializedKeys.Add(name);
            SerializedHashes.Add(name.Hash());
            SerializedValues.Add(value);
        }
        protected override void OnAddKeyAndValue()
        {
            SerializedHashes.Add(SerializedKeys[^1].Hash());
        }
        public void Add(T value)
        {
            if (IsReadOnly) return;
            Guid G = Guid.NewGuid();
            SerializedKeys.Add(G.ToString());
            SerializedHashes.Add(G.ToString().Hash());
            SerializedValues.Add(value);
        }
        public override void Add(KeyValuePair<string, T> item) => Add(item.Key, item.Value);

        public override void Remove(string name)
        {
            if (IsReadOnly || !SerializedKeys.Contains(name)) return;
            RemoveAt(IndexOf(name));
        }
        public override void RemoveAt(int i)
        {
            if (IsReadOnly || i < 0 || i >= SerializedValues.Count) return;
            SerializedKeys.RemoveAt(i);
            SerializedHashes.RemoveAt(i);
            SerializedValues.RemoveAt(i);
        }
        public override void Clear()
        {
            SerializedKeys.Clear();
            SerializedHashes.Clear();
            SerializedValues.Clear();
        }


        public bool ContainsHash(int i) => SerializedHashes.Contains(i);
        public bool Contains(int i) => ContainsHash(i);

        public int IndexOfHash(int i) => SerializedHashes.IndexOf(i);
        public int IndexOf(int i) => IndexOfHash(i);

        public Dictionary<string, T> ToNameDictionary() => SerializedKeys.Zip(SerializedValues, (n, v) => new { n, v }).ToDictionary(x => x.n, x => x.v);
        public Dictionary<string, T> ToKeyDictionary() => ToNativeDictionary();
        public Dictionary<string, int> ToHashDictionary() => SerializedKeys.Zip(serializedHashes, (n, k) => new { n, k }).ToDictionary(x => x.n, x => x.k);

        // Access to secondary integer hashes
        public IReadOnlyList<int> Hash => serializedHashes;
        public int HashOf(string name) => !SerializedKeys.Contains(name)
            ? name.Hash()
            : SerializedHashes[SerializedKeys.IndexOf(name)];
        public bool TryGetHash(string name, out int hash)
        {
            hash = default;
            if (!SerializedKeys.Contains(name)) return false;
            hash = serializedHashes[SerializedKeys.IndexOf(name)];
            return true;
        }
        public void Clone(HashedListSReference<T> source, DictionaryCloneOp op = DictionaryCloneOp.Transfer)
        {
            if (source == null) return;
            if (Count == 0) op = DictionaryCloneOp.TransferAndAdd;
            if (op is DictionaryCloneOp.ReplaceEntirely) Clear();
            for (int i = 0; i < source; i++)
                if (SerializedHashes.Contains(source.SerializedHashes[i]) || op is not DictionaryCloneOp.Transfer)
                    this[source.SerializedKeys[i]] = source.SerializedValues[i];
        }
    }
}
