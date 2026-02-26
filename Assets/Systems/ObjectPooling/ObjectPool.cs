using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace RageRooster.Systems.ObjectPooling
{
    /// <summary>
    /// An active <see cref="ObjectPool"/> in the game's memory, can be attached directly to a behavior or the <see cref="GlobalPool"/>.
    /// </summary>
    [System.Serializable, Inspectable]
    public class ObjectPool
    {
        [field: SerializeField] public string name { private set; get; }
        [field: SerializeField] public PoolableObject prefab { private set; get; }
        [field: SerializeField] public int initialSize { private set; get; } = 5;
        [field: SerializeField] public bool canGrow { private set; get; } = true;
        [field: SerializeField] public bool autoEnable { private set; get; } = true;
        [field: SerializeField] public float autoDisableTime { private set; get; } = -1;
        [field: SerializeField] public Transform poolParentOverride { private set; get; }
        [field: SerializeField] public bool orphanOnDestroy { private set; get; } = false;

        //Data
        [field: NonSerialized] public readonly List<PoolableObject> poolList = new();
        [field: NonSerialized] public int activeObjects { get; protected set; } = 0;
        [field: NonSerialized] public int currentIndex { get; protected set; } = 0;
        [field: NonSerialized] public bool initialized { get; protected set; } = false;
        [field: NonSerialized] public bool initializing { get; protected set; } = false;
        public int pooledObjects => poolList.Count;
        public Transform poolParent => poolParentOverride != null ? poolParentOverride : GlobalPool.poolParent;

        //Customizable Callbacks
        public Action<ObjectPool> onInitialize;
        public Action<PoolableObject> onCreateInstance;
        public Action<PoolableObject> onPreInstanceEnable;
        public Action<PoolableObject> onInstanceDisable;
        public Action onFailedPump;

        public virtual void Initialize()
        {
            if (initialized || initializing) return;
            initializing = true;
            InitializeEnum().Begin();
        }

        protected virtual IEnumerator InitializeEnum()
        {
            yield return NewInstanceEnum(initialSize);
            initialized = true;
            initializing = false;
            onInitialize?.Invoke(this);
            currentIndex = pooledObjects - 1; // Set to end of the list so that the first Pooling will use index 0.
        }

        protected virtual void NewInstance()
        {
            PoolableObject poolable = PoolableObject.Instantiate(prefab, poolParent);
            AfterNewInstance(poolable);
        }

        protected virtual IEnumerator NewInstanceEnum(int count = 1)
        {
            AsyncInstantiateOperation<PoolableObject> op = UnityEngine.Object.InstantiateAsync(prefab, count, poolParent);
            while (!op.isDone) yield return null;
            for (int i = 0; i < op.Result.Length; i++)
            {
                PoolableObject poolable = op.Result[i];
                AfterNewInstance(poolable);
            }
        }
        protected virtual void AfterNewInstance(PoolableObject newInstance)
        {
            newInstance.Initialize(this);
            poolList.Add(newInstance);
            onCreateInstance?.Invoke(newInstance);
        }



        public void Update(float delta)
        {
            if (autoDisableTime > 0)
                for (int i = 0; i < poolList.Count; i++)
                    if (poolList[i].Active && poolList[i].spawnTime + autoDisableTime <= delta)
                        poolList[i].Active = false;
        }

        public PoolableObject Pump()
        {
            if (!initialized) Initialize();

            IncrementSelection();

            //FindNext Instance
            PoolableObject instance = null;
            if (!poolList[currentIndex].Active) instance = poolList[currentIndex];
            else if (activeObjects >= pooledObjects)
            {
                if (canGrow)
                {
                    NewInstance();
                    currentIndex = pooledObjects - 1;
                    instance = poolList[currentIndex];
                }
            }
            else
            {
                int safetyCounter = 0;
                while (poolList[currentIndex].Active)
                {
                    IncrementSelection();
                    safetyCounter++;
                    if (safetyCounter > initialSize * 1000) break;
                }
            }

            if (instance != null && !instance.Active)
            {
                instance.Active = true;
                activeObjects++;

                onPreInstanceEnable?.Invoke(instance);
                if (autoEnable) instance.Active = true;
                return instance;
            }
            else
            {
                onFailedPump?.Invoke();
                return null;
            }

        }
        public bool Pump(out PoolableObject result)
        {
            result = Pump();
            return result != null;
        }

        public void Pump(Action<PoolableObject> result)
        {
            Enum().Begin();
            IEnumerator Enum()
            {
                if (!initialized) yield return InitializeEnum();

                IncrementSelection();

                //FindNext Instance
                PoolableObject instance = null;
                if (!poolList[currentIndex].Active) instance = poolList[currentIndex];
                else if (activeObjects >= pooledObjects)
                {
                    if (canGrow)
                    {
                        yield return NewInstanceEnum();
                        currentIndex = pooledObjects - 1;
                        instance = poolList[currentIndex];
                    }
                }
                else
                {
                    int safetyCounter = 0;
                    while (poolList[currentIndex].Active)
                    {
                        IncrementSelection();
                        safetyCounter++;
                        if (safetyCounter > initialSize * 1000) break;
                    }
                }

                if (instance != null && !instance.Active)
                {
                    instance.Active = true;
                    activeObjects++;

                    onPreInstanceEnable?.Invoke(instance);
                    if (autoEnable) instance.Active = true;
                    result.Invoke(instance);
                }
                else onFailedPump?.Invoke();
            }
        }

        protected virtual void IncrementSelection()
        {
            currentIndex++;
            if (currentIndex >= pooledObjects) currentIndex = 0;
            if (currentIndex > poolList.Count)
                Debug.Break();
        }



        /// <summary>
        /// Callback for when an instance in this pool has been disabled. Do not call outside of PoolableObject.
        /// </summary>
        /// <param name="obj"></param>
        public virtual void OnInstanceDisable(PoolableObject obj)
        {
            activeObjects--;
            onInstanceDisable?.Invoke(obj);
        }

        public virtual void DisableAll()
        {
            foreach (PoolableObject item in poolList) item.Active = false;
            activeObjects = 0;
            currentIndex = 0;
        }

        public virtual void Cleanup()
        {
            initialized = false;
            activeObjects = 0;
            currentIndex = 0;
            activeObjects = 0;
            for (int i = poolList.Count - 1; i >= 0; i--)
            {
                if (orphanOnDestroy) UnityEngine.Object.Destroy(poolList[i]);
                else
                {
                    poolList[i].Active = false;
                    UnityEngine.Object.Destroy(poolList[i].gameObject);
                }
            }
        }

    }

    /// <summary>
    /// An active <see cref="ObjectPool"/> in the game's memory, with additional tracking for a <see cref="MonoBehaviour"/> <see cref="Type"/>, <see cref="T"/>.
    /// <br/> Can be attached directly to a behavior or the <see cref="GlobalPool"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to be tracked.</typeparam>
    [System.Serializable, Inspectable]
    public class ObjectPool<T> : ObjectPool where T : MonoBehaviour
    {
        [NonSerialized] private List<T> componentList = new();

        protected override void AfterNewInstance(PoolableObject newInstance)
        {
            base.AfterNewInstance(newInstance);
            if (newInstance.TryGetComponent(out T comp)) componentList.Add(comp);
        }

        public new T Pump() => base.Pump() ? componentList[currentIndex] : null;
        public PoolableObject PumpBase() => base.Pump();

        public bool Pump(out T result)
        {
            if (Pump(out PoolableObject p))
            {
                result = componentList[currentIndex];
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
        public bool Pump(out PoolableObject resultP, out T resultT)
        {
            if (Pump(out resultP))
            {
                resultT = componentList[currentIndex];
                return true;
            }
            else
            {
                resultT = null;
                return false;
            }
        }

        public void Pump(Action<PoolableObject, T> result) => base.Pump(P => { result?.Invoke(P, componentList[currentIndex]); });

        public T GetCurrentIndexComponent() => componentList[currentIndex];
    }

    /// <summary>
    /// An active <see cref="ObjectPool"/> in the game's memory, with additional tracking for two <see cref="MonoBehaviour"/> <see cref="Type"/>s, <see cref="T1"/> and <see cref="T2"/>.
    /// <br/> Can be attached directly to a behavior or the <see cref="GlobalPool"/>.
    /// </summary>
    /// <typeparam name="T1">The first <see cref="Type"/> to be tracked.</typeparam>
    /// <typeparam name="T2">The second <see cref="Type"/> to be tracked.</typeparam>
    [System.Serializable, Inspectable]
    public class ObjectPool<T1, T2> : ObjectPool where T1 : MonoBehaviour where T2 : MonoBehaviour
    {
        [NonSerialized] private List<T1> componentList1 = new();
        [NonSerialized] private List<T2> componentList2 = new();


        protected override void AfterNewInstance(PoolableObject newInstance)
        {
            base.AfterNewInstance(newInstance);
            if (newInstance.TryGetComponent(out T1 comp1)) componentList1.Add(comp1);
            if (newInstance.TryGetComponent(out T2 comp2)) componentList2.Add(comp2);
        }

        public T1 Pump1() => base.Pump() ? componentList1[currentIndex] : null;
        public T2 Pump2() => base.Pump() ? componentList2[currentIndex] : null;
        public PoolableObject PumpBase() => base.Pump();

        public bool Pump(out T1 result)
        {
            if (Pump(out PoolableObject p))
            {
                result = componentList1[currentIndex];
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
        public bool Pump(out T2 result)
        {
            if (Pump(out PoolableObject p))
            {
                result = componentList2[currentIndex];
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
        public bool Pump(out T1 result1, out T2 result2)
        {
            if (Pump(out PoolableObject p))
            {
                result1 = componentList1[currentIndex];
                result2 = componentList2[currentIndex];
                return true;
            }
            else
            {
                result1 = null;
                result2 = null;
                return false;
            }
        }
        public bool Pump(out PoolableObject resultP, out T1 result1, out T2 result2)
        {
            if (Pump(out resultP))
            {
                result1 = componentList1[currentIndex];
                result2 = componentList2[currentIndex];
                return true;
            }
            else
            {
                result1 = null;
                result2 = null;
                return false;
            }
        }

        public void Pump(Action<PoolableObject, T1, T2> result) => base.Pump(P => { result?.Invoke(P, componentList1[currentIndex], componentList2[currentIndex]); });

        public T1 GetCurrentIndexComponent1() => componentList1[currentIndex];
        public T2 GetCurrentIndexComponent2() => componentList2[currentIndex];
    }
}

