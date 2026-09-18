using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using SLS.Singletons;
using SLS.ObjectUtilities.Projectiles;

namespace SLS.ObjectUtilities
{
    /// <summary>
    /// A global pool for pooled objects shared between multiple entities. Use a <see cref="GlobalPool.Client"/> to interface with this.
    /// </summary>
    public class GlobalPool : GlobalAsset<GlobalPool>
    {
        static GlobalPool instance;

        public static bool initialized { private set; get; }
        public static Transform poolParent;

        static Dictionary<string, ObjectPool> dictionary_string = new();
        static Dictionary<Spawnable, ObjectPool> dictionary_prefab = new();

        public List<ObjectPool> serializedPools = new();

        public ObjectPool basicEnemyBullet;
        public static ObjectPool BasicEnemyBullet;

        public void Initialize()
        {
            if (initialized && Active) return;
            if (!Application.isPlaying) return;

            if(poolParent != null)
            {
                ObjectPool.DefaultPoolParent = poolParent;
                UnityEngine.Object.DontDestroyOnLoad(ObjectPool.DefaultPoolParent);
            }

            InitPoolGlobally(basicEnemyBullet);
            BasicEnemyBullet = basicEnemyBullet;

            foreach (var item in serializedPools) InitPoolGlobally(item);

            UpdateProxy.OnUpdate += Update;
            initialized = true;
        }

        void Update()
        {
            basicEnemyBullet.Update(Time.deltaTime);
            for (int i = 0; i < serializedPools.Count; i++) serializedPools[i].Update(Time.deltaTime);
        }

        public override void OnDeInit()
        {
            initialized = false;
            UpdateProxy.OnUpdate -= Update;
            foreach (var pool in serializedPools) pool.Cleanup();
        }


        private void InitPoolGlobally(ObjectPool item)
        {
            if (item.prefab == null) return;
            item.Initialize();
            if (!string.IsNullOrEmpty(item.name)) dictionary_string.Add(item.name, item);
            dictionary_prefab.Add(item.prefab, item);
        }

        public static void UnloadAllPools()
        {
            if (!initialized) return;
            foreach (var pool in Get.serializedPools) pool.DisableAll();
        }

        public static ObjectPool GetPool(string poolName)
        {
            if (!initialized) return null;
            if (dictionary_string.TryGetValue(poolName, out ObjectPool pool)) return pool;
            return null;
        }
        public static ObjectPool GetPool(Spawnable prefab)
        {
            if (!initialized) return null;
            if (dictionary_prefab.TryGetValue(prefab, out ObjectPool pool)) return pool;
            return null;

        }

        public List<ObjectPool> ALLGlobalPools()
        {
            List<ObjectPool> res = new();
            for (int i = 0; i < serializedPools.Count; i++) res.Add(serializedPools[i]);
            res.Add(basicEnemyBullet);

            return res;
        }


        /// <summary>
        /// A <see cref="Client"/> of the <see cref="GlobalPool"/> system.
        /// </summary>
        [System.Serializable, Unity.VisualScripting.Inspectable]
        public class Client
        {
            [SerializeField, Unity.VisualScripting.Inspectable] MonoBehaviour owner;
            [SerializeField, Unity.VisualScripting.Inspectable] Spawnable prefab;
            [SerializeField, Unity.VisualScripting.Inspectable] Transform muzzle;
            private bool initialized;
            private ObjectPool pool;
            public Action<Spawnable> onPumpInstance;

            public Client(MonoBehaviour Owner = null, Spawnable Prefab = null, ObjectPool Pool = null)
            {
                if (Pool != null)
                {
                    pool = Pool;
                    prefab = Pool.prefab;
                }
                else if (Prefab != null) prefab = Prefab;
                if (Owner != null) owner = Owner;
            }

            public void Initialize()
            {
                if (initialized) return;
                pool = GetPool(prefab);
                pool.Initialize();
                initialized = true;
            }

            public Spawnable Pump(Placement? placement = null)
            {
                if (!initialized) Initialize();
                if (!placement.HasValue) placement = muzzle;
                var res = pool.Pump(placement.Value);
                onPumpInstance?.Invoke(res);
                res.currentClient = owner;
                return res;
            }

#if UNITY_EDITOR
            [CustomPropertyDrawer(typeof(GlobalPool.Client))]
            private class Editor : PropertyDrawer
            {
                public override UnityEngine.UIElements.VisualElement CreatePropertyGUI(SerializedProperty property)
                {
                    // Root foldout that contains the whole client UI
                    Foldout foldout = new();
                    foldout.text = property.displayName ?? "GlobalPool Client";
                    foldout.value = true; // expanded by default

                    // Ensure serializedObject is up to date
                    property.serializedObject.Update();

                    // Find relative properties
                    var ownerProp = property.FindPropertyRelative("owner");
                    var prefabProp = property.FindPropertyRelative("prefab");
                    var muzzleProp = property.FindPropertyRelative("muzzle");

                    // Auto-assign owner if null
                    if (ownerProp != null && ownerProp.objectReferenceValue == null)
                    {
                        var target = property.serializedObject.targetObject;
                        if (target is MonoBehaviour mb)
                        {
                            ownerProp.objectReferenceValue = mb;
                            property.serializedObject.ApplyModifiedProperties();
                        }
                        else if (target is Component comp)
                        {
                            var asMb = comp as MonoBehaviour;
                            if (asMb != null)
                            {
                                ownerProp.objectReferenceValue = asMb;
                                property.serializedObject.ApplyModifiedProperties();
                            }
                        }
                    }

                    // Horizontal row for prefab + selector button
                    VisualElement prefabRow = new()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center
                        }
                    };

                    // Prefab field
                    if (prefabProp != null)
                    {
                        PropertyField prefabField = new(prefabProp)
                        {
                            label = "Prefab",
                            style =
                            {
                                flexGrow = 1
                            }
                        };
                        prefabRow.Add(prefabField);
                    }

                    // Small selector button with a non-dynamic label "*"
                    Button selectorButton = null;
                    selectorButton = new(() => { PopupSelector(property, prefabProp, selectorButton); })
                    {
                        text = "*"
                    };

                    // Style selector button: small fixed width
                    selectorButton.style.width = 24;
                    selectorButton.style.marginLeft = 6;
                    selectorButton.style.marginRight = 0;
                    selectorButton.style.alignSelf = UnityEngine.UIElements.Align.FlexEnd;

                    prefabRow.Add(selectorButton);

                    // Add prefab row to foldout
                    foldout.Add(prefabRow);

                    // Add muzzle field below the prefab selector/chooser
                    if (muzzleProp != null)
                    {
                        var muzzleField = new UnityEditor.UIElements.PropertyField(muzzleProp);
                        muzzleField.label = "Muzzle";
                        foldout.Add(muzzleField);
                    }

                    return foldout;
                }

                void PopupSelector(SerializedProperty property, SerializedProperty prefabProp, Button button)
                {
                    // Build pool list on demand
                    List<ObjectPool> poolList = new();

                    // Prefer runtime instance
                    if (GlobalPool.Get != null)
                    {
                        try
                        {
                            poolList = GlobalPool.Get.ALLGlobalPools() ?? new List<ObjectPool>();
                        }
                        catch
                        {
                            poolList = new List<ObjectPool>();
                        }
                    }
                    else
                    {
                        // Fallback: search assets for GlobalPool assets
                        var guids = UnityEditor.AssetDatabase.FindAssets("t:GlobalPool");
                        for (int i = 0; i < guids.Length; i++)
                        {
                            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GlobalPool>(path);
                            if (asset != null)
                            {
                                try
                                {
                                    var pools = asset.ALLGlobalPools();
                                    if (pools != null)
                                    {
                                        for (int j = 0; j < pools.Count; j++)
                                        {
                                            if (!poolList.Contains(pools[j])) poolList.Add(pools[j]);
                                        }
                                    }
                                }
                                catch { /* ignore bad assets */ }
                            }
                        }
                    }

                    GenericDropdownMenu menu = new();
                    foreach (var pool in poolList)
                    {
                        menu.AddItem(pool.name, prefabProp.objectReferenceValue == pool.prefab, () =>
                        {
                            prefabProp.objectReferenceValue = pool.prefab;
                            property?.serializedObject?.ApplyModifiedProperties();
                        });
                    }
                    menu.DropDown(button.worldBound, button, false);
                }

            }
#endif
        }

        /*
        [MenuItem("File/CreateObjectPool")]
        private static void CREATE()
        {
            var instance = CreateInstance<GlobalPool>();
            AssetDatabase.CreateAsset(instance, "Assets/ObjectPools.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = instance;
        }*/
    }
}