using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using SLS.EditorUtilities.Editor;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace Utilities.ObjectPooling
{
    /// <summary>
    /// An active <see cref="ObjectPool"/> in the game's memory, can be attached directly to a behavior or the GlobalPool.
    /// Updated to work with the new Utilities.Spawnable API.
    /// </summary>
    [System.Serializable, Inspectable]
    public class ObjectPool
    {
        [field: SerializeField] public string name { protected set; get; }
        [field: SerializeField] public Spawnable prefab { protected set; get; }
        [field: SerializeField] public int initialSize { protected set; get; } = 5;
        [field: SerializeField] public bool canGrow { protected set; get; } = true;
        [field: SerializeField] public float autoDisableTime { protected set; get; } = -1;
        [field: SerializeField] public Transform parentOverride { protected set; get; }
        [field: SerializeField] public bool orphanOnDestroy { protected set; get; } = false;

        //Data
        [field: NonSerialized] public readonly List<Spawnable> poolList = new();
        [field: NonSerialized] public int activeObjects { get; protected set; } = 0;
        [field: NonSerialized] public int currentIndex { get; protected set; } = 0;
        [field: NonSerialized] public bool initialized { get; protected set; } = false;
        [field: NonSerialized] public bool initializing { get; protected set; } = false;
        public int pooledObjects => poolList.Count;
        public Transform poolParent => parentOverride != null ? parentOverride : DefaultPoolParent;
        public static Transform DefaultPoolParent;

        //Customizable Callbacks
        public Action<ObjectPool> onInitialize;
        public Action<Spawnable> onCreateInstance;
        public Action<Spawnable> onPreInstanceEnable;
        public Action<Spawnable> onInstanceDisable;
        public Action onFailedPump;

        public static ObjectPool NEW(Spawnable prefab, int initialSize = 5, bool canGrow = true, float autoDisableTime = -1, Transform poolParentOverride = null, bool orphanOnDestroy = false)
        {
            ObjectPool This = new();
            This.prefab = prefab;
            This.initialSize = initialSize;
            This.canGrow = canGrow;
            This.autoDisableTime = autoDisableTime;
            This.parentOverride = poolParentOverride;
            This.orphanOnDestroy = orphanOnDestroy;
            return This;
        }

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
            if (prefab == null) return;
            // Use the Spawnable.Instantiate API which expects a GameObject and a client (we pass poolParent as client)
            Spawnable poolable = Spawnable.Instantiate(prefab.gameObject, poolParent);
            AfterNewInstance(poolable);
        }

        protected virtual IEnumerator NewInstanceEnum(int count = 1)
        {
            if (prefab == null) yield break;

            for (int i = 0; i < count; i++)
            {
                Spawnable poolable = Spawnable.Instantiate(prefab.gameObject, poolParent);
                AfterNewInstance(poolable);
                // Spread creations across frames to avoid hitches
                if (i % 4 == 1) yield return null;
            }

            yield return null;
        }

        protected virtual void AfterNewInstance(Spawnable newInstance)
        {
            if (newInstance == null) return;

            // Register with pool list
            poolList.Add(newInstance);
            onCreateInstance?.Invoke(newInstance);

            // Maintain activeObjects count via spawnable's events.
            // onActivate increments, onDeactivate decrements and notifies onInstanceDisable.
            newInstance.onActivate += () =>
            {
                activeObjects++;
            };

            newInstance.onDeactivate += () =>
            {
                activeObjects = Math.Max(0, activeObjects - 1);
                onInstanceDisable?.Invoke(newInstance);
            };

            // If the spawnable starts as Prefab, ensure it's set to Inactive so pool can reuse it.
            if (newInstance.IsPrefab) newInstance.Despawn();
        }



        public void Update(float delta)
        {
            if (autoDisableTime > 0)
            {
                for (int i = 0; i < poolList.Count; i++)
                {
                    var s = poolList[i];
                    if (s == null) continue;
                    if (!s.Ready && s.spawnTime + autoDisableTime <= delta)
                    {
                        s.Despawn();
                    }
                }
            }
        }

        protected void IncrementSelection()
        {
            currentIndex++;
            if (currentIndex >= pooledObjects) currentIndex = 0;
        }

        public Spawnable Pump(Placement placement)
        {
            if (!initialized) Initialize();

            IncrementSelection();

            //FindNext Instance
            Spawnable instance = null;
            if (poolList.Count == 0)
            {
                if (canGrow)
                {
                    NewInstance();
                    currentIndex = pooledObjects - 1;
                }
            }

            if (poolList.Count > 0)
            {
                if (poolList[currentIndex].Ready) instance = poolList[currentIndex];
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
                    int maxSafety = Math.Max(1, initialSize) * 1000;
                    while (!poolList[currentIndex].Ready)
                    {
                        IncrementSelection();
                        safetyCounter++;
                        if (safetyCounter > maxSafety) break;
                    }

                    if (poolList[currentIndex].Ready)
                        instance = poolList[currentIndex];
                }
            }

            if (instance != null && instance.Ready)
            {
                onPreInstanceEnable?.Invoke(instance);
                instance.Spawn(placement);
                return instance;
            }
            else
            {
                onFailedPump?.Invoke();
                return null;
            }
        }
        public bool Pump(out Spawnable result, Placement placement)
        {
            result = Pump(placement);
            return result != null;
        }

        public void Pump(Action<Spawnable> result, Placement placement)
        {
            Enum().Begin();
            IEnumerator Enum()
            {
                if (!initialized) yield return InitializeEnum();

                IncrementSelection();

                //FindNext Instance
                Spawnable instance = null;
                if (poolList.Count == 0)
                {
                    if (canGrow)
                    {
                        yield return NewInstanceEnum(1);
                        currentIndex = pooledObjects - 1;
                        instance = poolList[currentIndex];
                    }
                }
                else
                {
                    if (poolList[currentIndex].Ready) instance = poolList[currentIndex];
                    else if (activeObjects >= pooledObjects)
                    {
                        if (canGrow)
                        {
                            yield return NewInstanceEnum(1);
                            currentIndex = pooledObjects - 1;
                            instance = poolList[currentIndex];
                        }
                    }
                    else
                    {
                        int safetyCounter = 0;
                        int maxSafety = Math.Max(1, initialSize) * 1000;
                        while (!poolList[currentIndex].Ready)
                        {
                            IncrementSelection();
                            safetyCounter++;
                            if (safetyCounter > maxSafety) break;
                        }

                        if (poolList[currentIndex].Ready)
                            instance = poolList[currentIndex];
                    }
                }

                if (instance != null && instance.Ready)
                {
                    onPreInstanceEnable?.Invoke(instance);
                    instance.Spawn(placement);
                    result?.Invoke(instance);
                }
                else
                {
                    onFailedPump?.Invoke();
                    result?.Invoke(null);
                }
            }
        }

        // Utility to destroy or disable through Spawnable API
        public static void DestroyOrDisable(GameObject subject)
        {
            Spawnable.DestroyOrDisable(subject);
        }


        public virtual void DisableAll()
        {
            foreach (Spawnable item in poolList) item.Despawn();
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
                    poolList[i].Despawn();
                    if (poolList[i].gameObject != null) UnityEngine.Object.Destroy(poolList[i].gameObject);
                    onFailedPump?.Invoke();
                    //result?.Invoke(null); //Not sure what the hell this is.
                }
            }
        }


#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ObjectPool), true)]
        private class Editor : UnityEditor.PropertyDrawer
        {

            Foldout primaryFoldout;
            Label label;

            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                VisualElement root = new();

                SerializedProperty initSizeProp = property.FindBackingFieldRelative(nameof(initialSize));


                primaryFoldout = new Foldout().AddTo(root, primaryFoldout =>
                {
                    primaryFoldout.text = property.displayName;
                    primaryFoldout.BindProperty(property);
                    label = primaryFoldout.Q<Label>(className: Foldout.textUssClassName);
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                });

                new PropertyField(property.FindBackingFieldRelative(nameof(prefab)), "").AddTo(label, t =>
                {
                    t.style.unityTextAlign = TextAnchor.UpperLeft;
                    t.style.minWidth = Length.Percent(75);
                    t.style.alignSelf = Align.FlexEnd;
                });

                new TextField("Title").AddTo(primaryFoldout, t =>
                {
                    t.BindProperty(property.FindBackingFieldRelative(nameof(ObjectPool.name)));
                });
                new VisualElement().AddTo(primaryFoldout, t =>
                {
                    t.style.flexDirection = FlexDirection.Row;

                    new IntegerField("Initial Size").AddTo(t, t2 =>
                    {
                        t2.BindProperty(initSizeProp);
                        t2.style.flexGrow = 1;
                    });

                    SerializedProperty canGrowProp = property.FindBackingFieldRelative(nameof(canGrow));
                    Button toggle = null; toggle = new Button(() => UpdateCanGrow(!canGrowProp.boolValue)).AddTo(t);
                    {
                        toggle.text = "+";
                        toggle.style.flexShrink = 1;
                        toggle.style.width = 20;
                        toggle.style.unityTextAlign = TextAnchor.MiddleCenter;
                        toggle.style.marginRight = -2.5f;
                        //toggle.BindProperty(canGrowProp);
                        toggle.style.backgroundColor = canGrowProp.boolValue ? new(.4f, .6f, .4f) : Color.clear;
                    }
                    void UpdateCanGrow(bool value)
                    {
                        canGrowProp.boolValue = value;
                        canGrowProp.serializedObject.ApplyModifiedProperties();
                        toggle.style.backgroundColor = value ? new(.4f, .8f, .4f) : Color.clear;
                    }

                });

                new FloatField("Auto Disable Time").AddTo(primaryFoldout, t =>
                {
                    t.BindProperty(property.FindBackingFieldRelative(nameof(parentOverride)));
                });
                new Toggle("Orphan On Pool Death").AddTo(primaryFoldout, t =>
                {
                    t.BindProperty(property.FindBackingFieldRelative(nameof(orphanOnDestroy)));
                });

                return root;
            }
        }
#endif
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

        public static new ObjectPool<T> NEW(Spawnable prefab, int initialSize = 5, bool canGrow = true, float autoDisableTime = -1, Transform poolParentOverride = null, bool orphanOnDestroy = false)
        {
            ObjectPool<T> This = new();
            This.prefab = prefab;
            This.initialSize = initialSize;
            This.canGrow = canGrow;
            This.autoDisableTime = autoDisableTime;
            This.parentOverride = poolParentOverride;
            This.orphanOnDestroy = orphanOnDestroy;
            return This;
        }

        protected override void AfterNewInstance(Spawnable newInstance)
        {
            base.AfterNewInstance(newInstance);
            if (newInstance.TryGetComponent(out T comp)) componentList.Add(comp);
        }

        public new T Pump(Placement placement) => base.Pump(placement) ? componentList[currentIndex] : null;
        public Spawnable PumpBase(Placement placement) => base.Pump(placement);

        public bool Pump(out T result, Placement placement)
        {
            if (Pump(out Spawnable p, placement))
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
        public bool Pump(out Spawnable resultP, out T resultT, Placement placement)
        {
            var result = base.Pump(out resultP, placement);
            resultT = componentList[currentIndex];
            return result;
        }
        public void Pump(Action<Spawnable, T> result, Placement placement) => base.Pump(P => { result?.Invoke(P, componentList[currentIndex]); }, placement);
        public T GetCurrentIndexComponent() => componentList[currentIndex];

    }
}