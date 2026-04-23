using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AYellowpaper
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="I">Interface Type</typeparam>
    /// <typeparam name="U">UnityEngine.Object Type</typeparam>
    [System.Serializable]
    public class InterfaceList<I, U> : INgb_InterfaceListNGB, IList, IList<I> where U : UnityEngine.Object where I : class
    {
        [SerializeField] internal List<U> list = new();
        IList INgb_InterfaceListNGB.listAccess => list;

        public I this[int i]
        {
            get => list[i] as I;
            set => list[i] = value as U;
        }

        public int Count => list.Count;

        public bool IsReadOnly { get; }
        public bool IsFixedSize { get; }
        public bool IsSynchronized { get; }
        public object SyncRoot { get; }
        

        object IList.this[int i]
        {
            get => list[i] as object;
            set => list[i] = value as U;
        }

        public void Add(I item) => list.Add(item as U);
        public void AddU(U item) => list.Add(item);
        public void AddUnique(I item) { if (!list.Contains(item as U)) list.Add(item as U); }
        public void AddUniqueU(U item) { if (!list.Contains(item)) list.Add(item); }
        public void Clear() => list.Clear();
        public bool Contains(I item) => list.Contains(item as U);
        public bool ContainsU(U item) => list.Contains(item);
        public void CopyTo(I[] array, int arrayIndex) => list.CopyTo(array as U[], arrayIndex);
        public void CopyToU(U[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
        public IEnumerator<U> GetUnityEnumerator() => list.GetEnumerator();
        public IEnumerator<I> GetInterfaceEnumerator() => list.GetEnumerator() as IEnumerator<I>;
        public int IndexOf(I item) => list.IndexOf(item as U);
        public int IndexOfU(U item) => list.IndexOf(item);
        public void Insert(int index, I item) => list.Insert(index, item as U);
        public void InsertU(int index, U item) => list.Insert(index, item);
        public bool Remove(I item) => list.Remove(item as U);
        public bool RemoveU(U item) => list.Remove(item);
        public void RemoveAt(int index) => list.RemoveAt(index);
        public void RemoveAtLast(int index = 1) => list.RemoveAt(list.Count - index);

        public void ClearNull()
        {
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i] == null)
                    list.RemoveAt(i);
        }

        public IEnumerator<I> GetEnumerator() => GetInterfaceEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Add(object value) => Add(value);
        public bool Contains(object value) => Contains(value);
        public int IndexOf(object value) => IndexOf(value);
        public void Insert(int index, object value) => Insert(index, value);
        public void Remove(object value) => Remove(value);
        public void CopyTo(Array array, int index) => CopyTo(array, index);
    }

    [System.Serializable]
    public class IComponentList<T> : InterfaceList<T, Component> where T : class { }
    [System.Serializable]
    public class IScriptableObjectList<T> : InterfaceList<T, ScriptableObject> where T : class { }

    public interface INgb_InterfaceListNGB
    {
        public IList listAccess { get; }
    }
}